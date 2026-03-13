# Basic Auths

Username/password credentials for a user. Password is hashed using `Corely.Security` hash providers.

## Model Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `UserId` | `Guid` | Associated user |
| `Password` | `IHashedValue` | Hashed password (never stored as plaintext) |
| `ModifiedUtc` | `DateTime?` | Last password change |

## Relationships

- **User** — 1:1 (each user has exactly one BasicAuth record)

## Key Behaviors

- Password is hashed on creation using the configured hash algorithm (default: Salted SHA-256)
- Verification uses `IHashedValue.Verify()` — timing-safe comparison
- Failed attempts increment `User.FailedLoginsSinceLastSuccess`
- After `MaxLoginAttempts` consecutive failures, `User.LockedUtc` is set (lockout)
- Lockout clears after `LockoutCooldownSeconds` or on successful login
- Self-ownership check (`IsAuthorizedForOwnUser`) allows users to verify/change their own password

## Result Codes

| Code | Meaning |
|------|---------|
| `CreateBasicAuthResultCode.Success` | Credentials created |
| `CreateBasicAuthResultCode.BasicAuthExistsError` | User already has credentials |
| `CreateBasicAuthResultCode.PasswordValidationError` | Password doesn't meet requirements |
| `VerifyBasicAuthResultCode.UserNotFoundError` | User not found |
