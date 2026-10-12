# Security policy in platform settings

**Status: built, not yet released.** Ships in Corely.IAM 4.0.0 with the password split.

## As built

Where the build differs from the plan below:

- **The switch is the saved row, not the platform account.** Saved settings apply when they exist,
  configuration otherwise. Bootstrap always writes them and only a platform account can save them, so
  this is the same rule, except that a platform account bootstrapped before 4.0 keeps running on
  configuration (not the defaults) until its Security page is saved. Opening the page and saving is
  the migration path; no separate import.
- **Reading works without a platform account.** `GetSecuritySettingsAsync` returns the settings in
  force wherever they come from; only `UpdateSecuritySettingsAsync` returns `NotFoundError` without
  one.
- **No password minimum against maximum check.** The two ranges meet at 64, so it can never fire; a
  unit test pins the ranges instead.
- **`ISecurityPolicyProvider` is public,** so `Corely.IAM.Web` reads the session lifetime for its
  cookies from it. `IPostAuthenticationFlowService.CompleteSignInAsync` lost its unused TTL argument.
- **`IPasswordValidationProvider` is no longer registered.** `SecurityPolicy.ValidatePassword` builds
  Corely.Security's validator from the policy per call.
- **The lockout test is integration, not functional:** it is service behavior, and the integration
  tier proves it first. The security page is covered by bUnit tests, since the functional tier cannot
  drive an interactive Blazor page.
- **The page shows durations in seconds** with each range beside the field, so a saved value is never
  rounded.
- **No database check constraints.** The EF configuration marks columns required; the limits are
  enforced by `SecurityPolicyValidator` at startup, on bootstrap and on save.
- **Two functional tests** used a 30 or 60 second token and a 300 second session, below the new
  minimums, and were raised to 300 and 3600; the fake clock makes the values arbitrary.

## What

Password rules, lockout, token lifetimes and the MFA settings become platform settings when the host
has a platform account. A platform member holding `platform_settings` changes them at runtime, and
every instance picks the change up within the settings cache window (30 seconds), with no restart.

## Where the values come from

One rule:

- **No platform account:** `appsettings.json`, through `SecurityOptions` and
  `PasswordValidationOptions`, as today.
- **A platform account:** the platform security settings row. Configuration is ignored.

Bootstrap writes the row, so a platform account always has one. When configuration still carries
any of these keys alongside a platform account, startup logs a warning naming them, once.

## Bootstrap

`corely-iam-db platform bootstrap` requires exactly one of:

- `--settings <path>`: reads the `SecurityOptions` and `PasswordValidationOptions` sections from that
  file and writes them as the row. Missing keys take the defaults. It reads only that file; the
  caller points it at the file holding the values the host really runs with.
- `--use-defaults`: writes the defaults.

Neither, or both, is refused with a message naming the two flags. There is no interactive prompt, so
scripted runs never hang. The values are validated as a save from the page is, and an invalid value
fails the bootstrap before anything is written.

## Settings and limits

Each setting has a minimum and a maximum, held as constants. Configuration, bootstrap and the page all
validate against them, and a unit test fails if any default falls outside its own range.

| Setting | Default | Min | Max | Configuration key |
|---------|---------|-----|-----|-------------------|
| Password minimum length | 8 | 3 | 64 | `PasswordValidationOptions:MinimumLength` |
| Password maximum length | 128 | 64 | 256 | `PasswordValidationOptions:MaximumLength` |
| Require uppercase | on | | | `PasswordValidationOptions:RequireUppercase` |
| Require lowercase | on | | | `PasswordValidationOptions:RequireLowercase` |
| Require digit | on | | | `PasswordValidationOptions:RequireDigit` |
| Require symbol | off | | | `PasswordValidationOptions:RequireNonAlphanumeric` |
| Failed sign ins before lockout | 5 | 3 | 20 | `SecurityOptions:MaxLoginAttempts` |
| Lockout length | 15 minutes | 1 minute | 1 day | `SecurityOptions:LockoutCooldownSeconds` |
| Sign in token lifetime | 1 hour | 5 minutes | 1 day | `SecurityOptions:AuthTokenTtlSeconds` |
| Session lifetime | 7 days | 1 hour | 90 days | `SecurityOptions:AuthSessionTtlSeconds` |
| MFA prompt timeout | 5 minutes | 1 minute | 15 minutes | `SecurityOptions:MfaChallengeTimeoutSeconds` |
| Recovery codes per user | 10 | 5 | 20 | `SecurityOptions:TotpRecoveryCodeCount` |

Cross checks: the minimum password length is no more than the maximum, and the token lifetime is no
longer than the session lifetime.

- The password minimum default rises from 5 to 8. The floor is 3 so demo systems can use short
  passwords; the page recommends 8 or more next to the field.
- A stricter password rule applies the next time a password is set. Stored passwords are never
  checked against it.
- `TotpRecoveryCodeCount` is not used today: `TotpAuthProcessor` always makes
  `TotpAuthConstants.DEFAULT_RECOVERY_CODE_COUNT` codes. This connects it.
- `GoogleClientId` and `PermissionCacheTtlSeconds` stay configuration only.

## Model

One model, `SecurityPolicy`, holds every setting above.

- **Configuration** binds the two sections and maps directly to it. A validator class checks it at
  startup, so out of range configuration fails the app on start.
- **The platform account** stores `PlatformSecuritySettingsEntity`, mapped to and from the model in
  the domain's `Mappers`. Its EF configuration takes column limits from the same constants, following
  the rest of the app.
- A unit test fails when a setting is on the model but not on the entity, or the reverse.

## Storage

A new table, `PlatformSecuritySettings`, one row with a fixed id, as `PlatformSettings` does for audit.
Columns are not nullable. A migration in both providers through `.\AddMigration.ps1`.

## Reading

One provider returns the `SecurityPolicy` in force, from the row when there is a platform account and
from configuration otherwise, through the existing per instance settings cache. Everything that reads
a moved setting from `IOptions<SecurityOptions>` or `IOptions<PasswordValidationOptions>` reads it
from the provider instead: sign in and lockout, token issue and renewal, MFA challenges, recovery code
generation, password validation, and the reauthentication checks from the password split.

Reading and saving for the portal go on `IPlatformService` (`GetSecuritySettingsAsync`,
`UpdateSecuritySettingsAsync`), gated on `platform_settings` like the audit settings methods on
`IAuditService`. Without a platform account both return a result saying so.

## Portal

The platform settings page splits into one page per group, with a shared left hand menu:

- `/platform-settings/audit`: the current audit settings, unchanged
- `/platform-settings/security`: everything above
- `/platform-settings` redirects to the first

Each page has its own Edit and Save. Saving is audited as an Update on `platform_settings`.

## Tests

- Unit: every limit and both cross checks, for the configuration validator and for a save; every
  default inside its range; model and entity carry the same settings; the provider returns
  configuration without a platform account and the row with one; the startup warning fires only when
  both are present; each consumer uses the provider's value rather than an option or a constant;
  bootstrap refuses neither flag and both flags, and refuses an invalid file without writing.
- Integration: the row round trips on SQLite and in the provider matrix.
- Functional: a saved lockout limit takes effect on the next sign in, and the security page saves.
- Migration CLI unit tests for both bootstrap flags.

## Docs

- `platform.md`: a "Where security settings come from" section with the rule above, the bootstrap
  flags, and the warning.
- `iam-options.md` and `authentication.md`: the settings stay listed, each section noting that a
  platform account takes over and linking to `platform.md`.
- The migration CLI docs: the two bootstrap flags.
- `Corely.IAM.Web/Docs`: the split settings pages.
- `MIGRATION-4.0.md`: bootstrap requires `--settings` or `--use-defaults`, and an existing platform
  account needs its row written (below).

## Rollout

An existing platform account has no row, and no migration can know its configuration. The only one
today is the DocsToData dev deployment. It runs the published Corely.IAM 3.5.0, so nothing can be
done there until DocsToData moves to 4.0: the table does not exist yet, and removing keys early
would change the running app. Checked when this was built:

- The App Service `admin-webapp-pinn-dev-doc2data-ue` (resource group `rg-pinn-dev-doc2data-ue`)
  carries no `SecurityOptions__*` or `PasswordValidationOptions__*` settings, and DocsToData's
  `iac/` and workflows set none. The deployment runs on the library defaults.
- Only `DocsToData.AdminPortalWebApp/appsettings.local.json` sets them: `MaxLoginAttempts` 5,
  `AuthTokenTtlSeconds` 3600, and relaxed password rules (minimum 5, no character requirements).
- `PortalFactory.cs` and `PortalBrowserFixture.cs` in DocsToData's tests set `SecurityOptions__*`.

When DocsToData moves to 4.0:

1. Run its database migration so the dev database gets `PlatformSecuritySettings`.
2. Dev deployment: sign in as a platform member and save `/platform-settings/security`. It starts
   from the values in force, the 4.0 defaults, so the only change from today is the password minimum
   rising from 5 to 8 for passwords set afterwards. No app settings to remove.
3. Local stack: if its database has a platform account, save the Security page there with the
   relaxed rules from `appsettings.local.json`, then remove the `SecurityOptions` (except
   `GoogleClientId` and `PermissionCacheTtlSeconds`) and `PasswordValidationOptions` keys from that
   file so startup logs no warning. Without a platform account the file stays as it is.
4. DocsToData's tests: keep the `SecurityOptions__*` values if those hosts run without a platform
   account; otherwise drop them. Check each value against the ranges either way.
