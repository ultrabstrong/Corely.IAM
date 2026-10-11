# Migrating to Corely.IAM 4.0

A signed in session alone can no longer change or remove a password. Changing one needs the current
password, and a user with TOTP enabled also needs a TOTP or recovery code to add, change or remove
one. There is no schema change.

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

## IModificationService has an authorization decorator

`ModificationServiceAuthorizationDecorator` sits between audit and the service. It refuses
`ModifyPasswordAsync` in system context and with no user context, and passes every other method
through unchanged.

## Corely.IAM.Web

The profile page asks for the current password to change or remove a password, and for an
authenticator code when TOTP is enabled.

## Corely.IAM.DataAccessMigrations.Cli

No change. It releases as 4.0.0 because its major tracks Corely.IAM's.
