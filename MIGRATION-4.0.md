# Migrating to Corely.IAM 4.0

Two changes need action. A signed in session alone can no longer change or remove a password, and
the security settings (password rules, lockout, token lifetimes, the two factor settings) have ranges,
and come from the platform account when there is one. One new table, `PlatformSecuritySettings`:
run `corely-iam-db db migrate`.

## SetPasswordAsync is split in two

`IRegistrationService.SetPasswordAsync` created a password when the user had none and overwrote it
when they did. It is removed, with `SetPasswordRequest`, `SetPasswordResult` and
`SetPasswordResultCode`. There is no alias.

```csharp
// 3.x
await registrationService.SetPasswordAsync(new SetPasswordRequest(newPassword));

// 4.0: a user with no password
await registrationService.RegisterPasswordAsync(new RegisterPasswordRequest(newPassword, totpCode));

// 4.0: a user with a password
await modificationService.ModifyPasswordAsync(
    new ModifyPasswordRequest(currentPassword, newPassword, totpCode));
```

`RegisterPasswordAsync` never overwrites: it returns `BasicAuthExistsError` when the user already
has a password. `ModifyPasswordAsync` returns `BasicAuthNotFoundError` when they have none.

`totpCode` is optional and needed only when the user has TOTP enabled. Without it the call returns
`TotpRequiredError`.

## Removing a password takes the current one

```csharp
// 3.x
await deregistrationService.DeregisterBasicAuthAsync();

// 4.0
await deregistrationService.DeregisterBasicAuthAsync(
    new DeregisterBasicAuthRequest(currentPassword, totpCode));
```

`DeregisterBasicAuthResultCode` gains `InvalidCurrentPasswordError`, `UserLockedError`,
`TotpRequiredError` and `InvalidTotpCodeError`, before `UnauthorizedError`, so its numeric values
changed. Compare by name, never by number.

## A wrong current password counts toward lockout

A wrong current password counts against `MaxLoginAttempts` as a failed sign in does. Once the user
is locked out, sign in, `ModifyPasswordAsync` and `DeregisterBasicAuthAsync` all refuse until
`LockoutCooldownSeconds` passes. The session that is already signed in stays signed in.

## Security settings have ranges

Each security setting has a range in `SecurityPolicyConstants`, and configuration outside it stops
the app starting. Check your `SecurityOptions` and `PasswordValidationOptions` against the table in
[where security settings come from](Corely.IAM/Docs/platform.md#where-security-settings-come-from).
The ones most likely to need a change: a token lifetime below 300 seconds, and a session lifetime
below 3600 seconds.

The default minimum password length rises from 5 to 8. It applies the next time a password is set;
stored passwords keep working. Set `PasswordValidationOptions:MinimumLength` to keep 5.

`SecurityOptions.TotpRecoveryCodeCount` now takes effect; it was ignored before.

## Security settings come from the platform account when there is one

Without a platform account nothing changes: configuration applies. With one, the platform's security
settings apply and these configuration keys are ignored.

- **Bootstrap** requires `--settings <path to appsettings.json>` or `--use-defaults`, and writes the
  settings from that file or the defaults.
- **A platform account bootstrapped before 4.0** has no saved settings. Configuration keeps applying
  to it, and startup logs a warning, until a platform member saves the Security page under
  `/platform-settings/security`. The page starts from the values in force, so opening it and saving
  carries them over.
- **After saving,** remove the keys from configuration. Startup logs a warning naming any that remain.

## API changes

- `IPlatformService` gains `GetSecuritySettingsAsync` and `UpdateSecuritySettingsAsync`, and an
  authorization decorator that gates them on `platform_settings` in the platform account.
- `ISecurityPolicyProvider` returns the security settings in force. Read lifetimes and limits from it
  rather than from `IOptions<SecurityOptions>`, which no longer reflects a platform account.
- `IPasswordValidationProvider` and `IOptions<PasswordValidationOptions>` are no longer registered.
  `SecurityPolicy.ValidatePassword` replaces them inside the library.
- `TotpAuthConstants.DEFAULT_RECOVERY_CODE_COUNT` is removed; use
  `SecurityPolicyConstants.TOTP_RECOVERY_CODE_COUNT_DEFAULT`.
- `BootstrapPlatformResultCode` gains `InvalidSecuritySettingsError`.
- `ListAuditAccountsResult` gains a last positional parameter, `DeletedAccounts`: the deleted
  accounts that still have audit entries, filled only for a viewer who reaches everything.

## Existing owners get missing owner defaults at startup

At startup, every account's Owner role is given the owner default of any registered type it has none
for, including host types registered after the account was created. Expect a one time log line,
"Gave existing Owner roles N missing owner default permissions", on the first start after
upgrading. Nothing is ever removed, and an owner default whose actions changed is left as it was. See
[Owner defaults](Corely.IAM/Docs/resource-types.md#owner-defaults).

## IModificationService has an authorization decorator

`ModificationServiceAuthorizationDecorator` sits between audit and the service. It refuses
`ModifyPasswordAsync` in system context and with no user context, and passes every other method
through unchanged.

## Corely.IAM.Web

- The profile page asks for the current password to change or remove a password, and for an
  authenticator code when TOTP is enabled.
- `IPostAuthenticationFlowService.CompleteSignInAsync` drops its unused TTL argument:
  `CompleteSignInAsync(httpContext, signInResult)`.
- The platform settings split into `/platform-settings/audit` and `/platform-settings/security`.
  `/platform-settings` still works and opens the first.

## Corely.IAM.DataAccessMigrations.Cli

`platform bootstrap` requires `--settings` or `--use-defaults`. It releases as 4.0.0 because its
major tracks Corely.IAM's.
