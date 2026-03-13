# Users

Host-agnostic user model with M:M account membership, login tracking, and per-user encryption keys.

## Model Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `Username` | `string` | Unique login name |
| `Email` | `string` | Email address |
| `LockedUtc` | `DateTime?` | Account lock timestamp (null = unlocked) |
| `TotalSuccessfulLogins` | `int` | Lifetime successful login count |
| `LastSuccessfulLoginUtc` | `DateTime?` | Most recent successful login |
| `FailedLoginsSinceLastSuccess` | `int` | Consecutive failed attempts |
| `TotalFailedLogins` | `int` | Lifetime failed login count |
| `LastFailedLoginUtc` | `DateTime?` | Most recent failed login |
| `CreatedUtc` | `DateTime` | Creation timestamp |
| `ModifiedUtc` | `DateTime?` | Last modification timestamp |
| `SymmetricKeys` | `List<SymmetricKey>?` | User encryption keys (hydrated) |
| `AsymmetricKeys` | `List<AsymmetricKey>?` | User encryption/signature keys (hydrated) |
| `Accounts` | `List<ChildRef>?` | Accounts this user belongs to (hydrated) |
| `Groups` | `List<ChildRef>?` | Groups this user is in (hydrated) |
| `Roles` | `List<ChildRef>?` | Roles assigned to this user (hydrated) |

## Relationships

- **Accounts** — M:M (users exist independently of accounts)
- **Groups** — M:M (scoped to the current account)
- **Roles** — M:M (scoped to the current account)
- **BasicAuth** — 1:1 (password credentials)

## Key Behaviors

- Users exist independently of accounts — there is no concept of "user A administrates user B"
- Account owners can add/remove users from their account but cannot read or modify other users directly
- Login metrics are updated on every authentication attempt
- User deletion requires no sole-ownership of any account (checked via `IsSoleOwnerOfAccountResult`)

## UserContext

```csharp
public record UserContext(
    User User,
    Account? CurrentAccount,
    string DeviceId,
    List<Account> AvailableAccounts);
```

Set by the host after authentication. Used by all IAM services for authorization context.

## Result Codes

| Code | Meaning |
|------|---------|
| `CreateUserResultCode.Success` | User created |
| `CreateUserResultCode.UserExistsError` | Duplicate username |
| `DeleteUserResultCode.UserIsSoleAccountOwnerError` | Cannot delete sole owner |
