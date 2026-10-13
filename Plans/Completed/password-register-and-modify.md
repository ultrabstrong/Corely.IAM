# Split SetPasswordAsync into RegisterPasswordAsync and ModifyPasswordAsync

**Status: done.** Released in Corely.IAM 4.0.0.

## As built

The three open questions were answered yes. Where the build differs from the plan below:

- **No options.** The two `SecurityOptions` switches were built and then removed: a user who turned
  on TOTP asked for the check, and no host needs the current password skipped today. Adding a switch
  later is not a breaking change.
- **`UserLockedError`** is added to `ModifyPasswordResultCode` and `DeregisterBasicAuthResultCode`:
  counting wrong passwords toward lockout needs a code for the locked state.
- **The checks live in one internal `IReauthenticationProvider`** (`Security/Providers`), called by
  all three services. It duplicates the few lines of lockout counting in `AuthenticationService`
  rather than refactoring sign in.
- **A missing current password is refused without counting**; only a wrong one counts. TOTP failures
  do not count, as at sign in.
- **The telemetry decorators no longer log these requests**, since they carry passwords.
  `SetPasswordAsync` logged its request at trace level.
- **`PasswordSection` reads TOTP status itself** through `IMfaService` on each parameter set, so the
  profile page needs no new parameter. "Change Password" is offered whenever the user has a password.
- **DevTools tests were not added.** Its commands are internal with no `InternalsVisibleTo` or Moq in
  the test project, and the argument binding is the shared `CommandBase` reflection.

Known edge: the TOTP check runs before the new password is validated, so a recovery code is spent
even when the new password then fails the rules.

## Why

`IRegistrationService.SetPasswordAsync` is an upsert. It creates a password when the user has none and
overwrites the existing one when they do (`RegistrationService.cs`, `SetPasswordAsync`). Three problems:

1. **Overwriting a password needs nothing but a signed in session.** No current password, no 2FA
   code. A hijacked session can take over the account by changing its password. The profile page
   (`PasswordSection.razor`) calls it the same way.
2. **An implementer cannot add the guard themselves.** The public API has no way to check a current
   password or a TOTP code short of a full sign in, which creates a session, writes audit entries and
   replaces the user context. Leaving the guard to the host only works if the host can build it.
3. **It blends create and update, and "Set" matches nothing else.** The services use Register and
   Modify (`RegisterUserAsync`, `ModifyUserAsync`).

The library already guards comparable operations itself: `DisableTotpAsync` requires a code, granting
is bounded, and self operations refuse system context. Password change is the odd one out.

## What changes

### Corely.IAM

**`IRegistrationService.RegisterPasswordAsync(RegisterPasswordRequest)`** adds a first password.

- `RegisterPasswordRequest(string Password, string? TotpCode = null)`
- Fails with `BasicAuthExistsError` when the user already has a password. It never overwrites.
- When the user has TOTP enabled, requires a valid TOTP or recovery code (see open question 1).
- Service authorization stays `IsNonSystemUserContext()`.
- Audit: Create on `user`, outside any account, as `SetPasswordAsync` is today.

**`IModificationService.ModifyPasswordAsync(ModifyPasswordRequest)`** updates an existing password.

- `ModifyPasswordRequest(string CurrentPassword, string NewPassword, string? TotpCode = null)`
- Fails with `BasicAuthNotFoundError` when the user has no password ("No password to update").
- Verifies `CurrentPassword` with `IBasicAuthProcessor.VerifyBasicAuthAsync` when
  `SecurityOptions.RequireCurrentPasswordToModifyPassword` is on.
- Verifies `TotpCode` with `ITotpAuthProcessor.VerifyTotpOrRecoveryCodeAsync` when the user has TOTP
  enabled and `SecurityOptions.RequireTotpToModifyPassword` is on. A recovery code is consumed, as at
  sign in.
- Returns its own `ModifyPasswordResult`, not the shared `ModifyResult`, because the caller has to
  tell these apart:

  | `ModifyPasswordResultCode` | When |
  |---|---|
  | `Success` | |
  | `BasicAuthNotFoundError` | User has no password |
  | `InvalidCurrentPasswordError` | Current password missing or wrong |
  | `TotpRequiredError` | TOTP enabled and no code given |
  | `InvalidTotpCodeError` | Code given but wrong |
  | `PasswordValidationError` | New password fails the rules |
  | `ValidationError` | |
  | `UnauthorizedError` | No user context, or system context |

- `IModificationService` has no authorization decorator today. Add
  `ModificationServiceAuthorizationDecorator`, registered between audit and the service, gating
  `ModifyPasswordAsync` on `IsNonSystemUserContext()` and passing every other method straight through.
- Audit: Update on `user`, outside any account. Add the method to `ModificationServiceAuditDecorator`
  and its telemetry decorator.

**`SecurityOptions`** gains two settings, both defaulting to `true`:

```csharp
public bool RequireCurrentPasswordToModifyPassword { get; set; } = true;
public bool RequireTotpToModifyPassword { get; set; } = true;
```

A host that wants a looser rule turns one off. The default is the strict one.

**Removed:** `SetPasswordAsync`, `SetPasswordRequest`, `SetPasswordResult`, `SetPasswordResultCode`.
No alias. The upsert is the thing being removed.

### Corely.IAM.Web

`PasswordSection.razor` already renders three states (no password, password only, password and
Google).

- **No password:** calls `RegisterPasswordAsync`. Shows a code input when TOTP is enabled.
- **Has a password:** the change form gains a current password input, plus a code input when TOTP is
  enabled, and calls `ModifyPasswordAsync`. It shows the current password input only when the option
  is on, which means the component needs to read `SecurityOptions`.
- Map each result code to a plain message ("Current password is incorrect", "Enter the code from
  your authenticator app").

### Corely.IAM.DevTools

Not published. New `password` command group, alongside `totp`:

```
password register <new> [code]
password modify <current> <new> [code]
```

Both sign in from the auth token file as the `totp` commands do.

### Corely.IAM.DataAccessMigrations.Cli

No code change. Its major must equal Corely.IAM's, so it releases as 4.0.0 alongside.

## Tests

Unit tier, following the existing layout:

- `RegistrationServiceTests`: register succeeds with no password; fails when one exists; with TOTP
  enabled, fails without a code, fails with a wrong one, succeeds with a valid code and with a
  recovery code.
- `ModificationServiceTests`: one test per result code in the table; each option turned off skips its
  check.
- Authorization decorator tests for both methods: user context passes, system context and no context
  are refused.
- `AuditDecoratorMappingTests` and `AuditDecoratorCoverageTests` cover the new and removed methods
  (coverage fails on its own until the audit decorator has the method).
- `Corely.IAM.Web.UnitTests`: `PasswordSection` shows the current password and code inputs only when
  they apply, and calls the right method in each state.
- DevTools unit tests for the two commands' argument handling.

Nothing here needs the integration or functional tier. Verification of the password and the code
already has its own tests at the processor level.

## Docs

- `Corely.IAM/Docs/services/registration.md`: replace the `SetPasswordAsync` row.
- `Corely.IAM/Docs/services/modification.md`: add `ModifyPasswordAsync` and the two options.
- `Corely.IAM/Docs/result-codes.md`: replace `SetPasswordResultCode`; add
  `RegisterPasswordResultCode` and `ModifyPasswordResultCode`.
- `Corely.IAM/Docs/google-signin.md`: "add a password via `RegisterPasswordAsync`".
- `Corely.IAM.Web/Docs/pages/profile.md`: the set and change flows.
- `MIGRATION-4.0.md` at the repository root: the rename, the split, the new required fields, and the
  two options for anyone who wants the old leniency back.

## Releases

| Package | From | To |
|---|---|---|
| Corely.IAM | 3.5.0 | 4.0.0 |
| Corely.IAM.Web | 3.5.1 | 4.0.0 |
| Corely.IAM.DataAccessMigrations.Cli | 3.1.0 | 4.0.0 |

Release Corely.IAM first. Nothing outside this repository calls `SetPasswordAsync` (Corely.Billing
and DocsToData checked), so they only need the package bump.

## Open questions

1. **Does registering a first password require a 2FA code when TOTP is enabled?** Recommended yes:
   adding a way to sign in is as sensitive as changing one. A Google only user with TOTP enabled would
   enter a code to add a password.
2. **Does a wrong current password count toward the sign in lockout?** Without it, the profile page
   lets a hijacked session guess the password with no limit. Recommended yes: count it against
   `MaxLoginAttempts` the way a failed sign in does.
3. **Does removing a password get the same guard?** `IDeregistrationService.DeregisterBasicAuthAsync()`
   takes no arguments, so a signed in session can remove the password of a user who also has Google
   sign in, with no current password and no code. Recommended: same current password and code checks
   and the same two options, in this plan, since it is the same major version.

## After this

`demo-quick-sign-in.md`, which uses `password modify` to give the seeded platform owner `Test1234`.
