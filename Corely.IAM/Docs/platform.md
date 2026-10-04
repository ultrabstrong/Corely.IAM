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
   nav bar does.

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
corely-iam-db platform bootstrap --email owner@example.com --secrets .\platform-owner.txt
```

It creates, in one go:

- a dedicated owner user (`platform-owner` unless `--username` says otherwise) with a generated 32
  character password and two factor sign in already enrolled;
- the account (`Platform` unless `--account-name` says otherwise), flagged as the platform account,
  with that user as its owner;
- full access on every IAM resource type for the account's Owner role.

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
- **The Owner role keeps the usual rule:** the platform account always has at least one owner.
- **Ordinary accounts are unaffected.** Their owners' "Account : Full Access" still means "this
  account". Only permissions held in the flagged account reach other accounts.

## Not yet

These follow before the platform account is relied on in production:

- an audit log of every action a platform member takes outside the platform account;
- requiring two factor sign in for every platform member, not only the bootstrapped owner;
- whether customers can see that a platform admin acted in their account.

## API

| Member | Purpose |
|--------|---------|
| `Account.IsPlatformAccount` | Marks the platform account |
| `IPlatformService.BootstrapPlatformAsync(BootstrapPlatformRequest)` | What the CLI command runs; returns the owner's credentials |
| `IPlatformService.CompletePlatformOwnerPermissionsAsync()` | Gives the platform Owner role any missing full access rows; returns how many it added |
| `EffectiveRole.ViaPlatformAccount` | True when a role in effective permissions comes from the platform account |
| `DeregisterAccountResultCode.PlatformAccountError` | Refusal to delete the platform account |
