# Platform Account

A platform account lets the people who run an application administer every account in it: provision
what customers can't give themselves, look into a customer account to help, or manage accounts as a
whole. It is optional. An application that never bootstraps one behaves exactly as before.

## What it is

The platform account is an ordinary account with one difference: `Account.IsPlatformAccount` is
`true`. It has users, roles, groups, permissions and invitations like any other account. There is at
most one, and only the bootstrap command creates it.

A **platform member** is any member of that account. What a platform member can do comes entirely
from their roles there, the same way access works in every account. Two rules make those roles reach
other accounts:

1. **Platform permissions apply everywhere.** Acting in any account, a user holds their permissions in
   that account plus their permissions in the platform account. Every check (`IsAuthorizedAsync`, the
   `CanGrant*` methods, the grantable lookups) sees both. A platform member who also belongs to a
   customer account keeps what they hold there too.
2. **Account Read in the platform account opens every account.** A platform member holding Read on
   `account` (on every resource) sees every account in the account list and can switch into any of
   them without being a member. They do not appear in that account's user list. Without it, their
   platform permissions only apply in accounts they already belong to.
   `UserContext.AvailableAccounts` still holds only their memberships plus the account they are in,
   so a host deciding whether to offer an account switch asks `ListAccountsAsync`, as the IAM.Web
   nav bar does. `UserContext.EnteredAsPlatformMember` is `true` in an account they entered this way,
   and `UserContext.MemberAccounts` leaves that account out.

Because a platform member's access is ordinary permissions, the rest of IAM needs no special cases:
the grant only what you hold rule bounds what platform members can hand out, and a slim role gives a
slim reach.

## Roles to build

The platform account's owner holds full access on every registered resource type, so they can build
any role. Keep that owner for rare use and give day to day work narrower roles.

| Role | Permissions in the platform account | Can, in any account |
|------|-------------------------------------|---------------------|
| Platform owner (bootstrapped) | Full access on every type | Everything |
| Grant provisioner | Account : Read, Grant : Read & Create | See every account and its grants, add grants. Nothing else |
| Support viewer | Account : Read, User : Read, Role : Read | Look into any account's users and roles, change nothing |
| Account manager | Account : Read & Update | Open any account, rename it, manage its invitations |

`grant` is an example of a host type (from Corely.Billing); any registered type works the same way.
Read on a type is needed to see its records, so a role that creates them should usually read them too.

## Enabling it

### 1. Bootstrap once

Run the migration CLI's bootstrap command against the application's database, after `db migrate`:

```powershell
$env:CORELY_IAM_DB_PROVIDER = "MsSql"
$env:CORELY_IAM_DB_CONNECTION = "<connection string>"
$env:CORELY_IAM_SYSTEM_KEY = "<the application's system key>"
corely-iam-db platform bootstrap --email owner@example.com --secrets .\platform-owner.txt `
    --settings ..\MyApp\appsettings.json
```

It creates, in one go:

- a dedicated owner user (`platform-owner` unless `--username` says otherwise) with a generated 32
  character password and two factor sign in already enrolled;
- the account (`Platform` unless `--account-name` says otherwise), flagged as the platform account,
  with that user as its owner;
- full access on every IAM resource type for the account's Owner role;
- the platform's security settings, from `--settings` or `--use-defaults`. Exactly one is required;
  see [where security settings come from](#where-security-settings-come-from).

The owner's username, password, authenticator secret, setup URI and recovery codes go to the file
named by `--secrets`. The command refuses to run without it, refuses a file that already exists, and
prints nothing secret. Add the secret to an authenticator app, store the file somewhere safe, and
delete it. Running the command again reports that the platform account exists and changes nothing.

The system key must be the application's own: IAM encrypts each user's keys with it, so an owner
created under any other key cannot sign in.

### 2. Let startup finish it

The CLI does not load the application, so it cannot know the resource types the application
registers (`grant`, `extraction` and so on). Every time an application calling `AddIAMServices` starts,
a hosted service gives the platform Owner role full access on any registered type it is missing. Most
starts this is one query that finds everything in place; a type registered later is picked up on the
next start. Nothing happens when there is no platform account.

The same work is available as `IPlatformService.CompletePlatformOwnerPermissionsAsync()`.

## Using it

1. **Sign in as the platform owner** with the generated password and an authenticator code.
2. **Invite platform admins.** They sign up as ordinary users first, then accept an invitation to the
   platform account, exactly as for any account.
3. **Give them slim roles.** Create the roles above (or your own) in the platform account and assign
   them. The owner can only hand out what it holds, which is everything; a platform admin with a slim
   role can only hand out that slim set.
4. **Work in a customer account.** A platform admin with Account Read picks the account from the
   account list and switches into it. Pages work as they do for the account's own members, limited
   to what the platform roles allow. The Your access panel marks each role that comes from the
   platform account.

## Protections

- **The platform account cannot be deleted.** Deregistering it returns `PlatformAccountError`, under
  system context too.
- **There is only one,** created only by bootstrap; nothing in the API sets or clears the flag.
- **Platform members sign in with two factor.** Entering the platform account, or entering an account
  through platform reach, needs TOTP enabled: signing in to it, switching to it and renewing a token in
  it return `TwoFactorRequiredError` otherwise. A member who turns TOTP off loses access at the next
  renewal. Members of an ordinary account are unaffected.
- **The Owner role keeps the usual rule:** the platform account always has at least one owner.
- **Ordinary accounts are unaffected.** Their owners' "Account : Full Access" still means "this
  account". Only permissions held in the flagged account reach other accounts.
- **Customers see what platform members did.** A platform member acting in an account they do not
  belong to is recorded in the [audit log](auditing.md) as a platform member, in that account, as far
  as the account's own audit settings choose. An account that switches platform members off gets
  nothing recorded for them.

## Platform settings

The platform account holds the platform settings, read and changed with `platform_settings`: whether
auditing is on, the longest retention, the actions accounts may record, and what is recorded for
system context and outside any account. Only a grant held in the platform account counts. A platform
member holding Read on `audit` there reads every entry, and Delete on `audit` there purges any
account's entries and those outside any account. See [Auditing](auditing.md).

The portal splits them into pages under `/platform-settings`: Audit, and Security.

## Where security settings come from

Password rules, lockout, token lifetimes and the two factor settings come from one of two places:

| The host has | Values come from |
|--------------|------------------|
| No platform account | Configuration: `SecurityOptions` and `PasswordValidationOptions` |
| A platform account | The platform's security settings. Configuration is ignored |

Bootstrap writes the platform's security settings, so a platform account always has them. A platform
member holding Update on `platform_settings` changes them on the Security page, or in code under
system context:

```csharp
var current = (await platformService.GetSecuritySettingsAsync()).Settings!;
await platformService.UpdateSecuritySettingsAsync(current with { MaxLoginAttempts = 3 });
```

Every instance picks up a change within `AuditOptions.SettingsCacheTtlSeconds` (30 by default). A
stricter password rule applies the next time a password is set, never to a stored password.

| Setting | Default | Range | Configuration key |
|---------|---------|-------|-------------------|
| Password minimum length | 8 | 3 to 64 | `PasswordValidationOptions:MinimumLength` |
| Password maximum length | 128 | 64 to 256 | `PasswordValidationOptions:MaximumLength` |
| Require uppercase, lowercase, digit | on | | `PasswordValidationOptions:RequireUppercase`, `RequireLowercase`, `RequireDigit` |
| Require symbol | off | | `PasswordValidationOptions:RequireNonAlphanumeric` |
| Failed sign ins before lockout | 5 | 3 to 20 | `SecurityOptions:MaxLoginAttempts` |
| Lockout length | 900 seconds | 60 to 86400 | `SecurityOptions:LockoutCooldownSeconds` |
| Sign in token lifetime | 3600 seconds | 300 to 86400, and no longer than the session | `SecurityOptions:AuthTokenTtlSeconds` |
| Session lifetime | 604800 seconds | 3600 to 7776000 | `SecurityOptions:AuthSessionTtlSeconds` |
| Two factor prompt timeout | 300 seconds | 60 to 900 | `SecurityOptions:MfaChallengeTimeoutSeconds` |
| Recovery codes per user | 10 | 5 to 20 | `SecurityOptions:TotpRecoveryCodeCount` |

The limits are `SecurityPolicyConstants`. Configuration is checked against them when the app starts,
and an out of range value stops it starting. A save outside them returns `ValidationError`. A minimum
password length below 8 suits a demo, not production.

Startup logs a warning when configuration still carries any of these keys alongside saved settings,
naming the keys to remove. It also warns when a platform account has no saved settings, which happens
only for one bootstrapped before this rule existed: configuration applies to it until someone saves
the Security page. `ISecurityPolicyProvider.GetPolicyAsync()` returns the settings in force, from
whichever place they come from.

## API

| Member | Purpose |
|--------|---------|
| `Account.IsPlatformAccount` | Marks the platform account |
| `IPlatformService.BootstrapPlatformAsync(BootstrapPlatformRequest)` | What the CLI command runs; returns the owner's credentials |
| `IPlatformService.CompletePlatformOwnerPermissionsAsync()` | Gives the platform Owner role any missing full access rows; returns how many it added |
| `IPlatformService.GetSecuritySettingsAsync()` | The security settings in force; needs Read on `platform_settings` in the platform account |
| `IPlatformService.UpdateSecuritySettingsAsync(SecurityPolicy)` | Saves them; needs Update there. `NotFoundError` without a platform account |
| `ISecurityPolicyProvider.GetPolicyAsync()` | The security settings in force, with no permission check, for code that applies them |
| `BootstrapPlatformResultCode.InvalidSecuritySettingsError` | The configured security settings are out of range; nothing was written |
| `EffectiveRole.ViaPlatformAccount` | True when a role in effective permissions comes from the platform account |
| `DeregisterAccountResultCode.PlatformAccountError` | Refusal to delete the platform account |
