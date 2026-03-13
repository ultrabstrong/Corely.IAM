# Authentication

JWT-based authentication with custom claims, device tracking, and multi-account support. No HttpContext dependency — works in any .NET host.

## Features

- **JWT tokens** — signed with per-user asymmetric keys, validated on every request
- **Custom claims** — `account_id`, `signed_in_account_id`, `device_id` embedded in token
- **Account switching** — issue new tokens scoped to a different account without re-authenticating
- **Device tracking** — tokens bound to device IDs for session management
- **Login metrics** — failed attempt counting and lockout cooldown
- **Bulk sign-out** — revoke all tokens for a user across all devices

## Sign-In Flow

1. User submits username + password to `IAuthenticationService.SignInAsync()`
2. `BasicAuthProcessor` validates credentials against hashed password
3. On success, `AuthenticationProvider` generates a JWT signed with the user's private key
4. Token includes claims: `sub` (user ID), `jti` (token ID), `iat`, `deviceId`, `accountId` (one per available account)
5. A tracking record is created in the database for revocation support
6. Returns `SignInResult` with token string and token ID

```csharp
var result = await authenticationService.SignInAsync(
    new SignInRequest("jdoe", "P@ssw0rd!", deviceId));

if (result.ResultCode == SignInResultCode.Success)
{
    var token = result.AuthToken;
    var tokenId = result.AuthTokenId;
}
```

## Account Switching

After sign-in, switch the active account without re-entering credentials:

```csharp
var result = await authenticationService.SwitchAccountAsync(
    new SwitchAccountRequest(targetAccountId));
```

This revokes the previous token and issues a new one with `signedInAccountId` set to the target account.

## Sign-Out

```csharp
// Single session
await authenticationService.SignOutAsync(new SignOutRequest(tokenId, deviceId));

// All sessions for the current user
await authenticationService.SignOutAllAsync();
```

`SignOutAsync` revokes the specific token. `SignOutAllAsync` revokes all active tokens for the user across all devices.

## Token Validation

Set user context by validating a token through `IUserContextProvider`:

```csharp
var contextProvider = serviceProvider.GetRequiredService<IUserContextProvider>();
var result = await contextProvider.SetUserContextAsync(token);

if (result == UserAuthTokenValidationResultCode.Success)
{
    var context = contextProvider.GetUserContext();
}
```

Validation checks: JWT format, required claims, user existence, token not revoked, token not expired, signature validity.

## Token Claims

| Claim | Type | Description |
|-------|------|-------------|
| `sub` | `Guid` | User ID |
| `jti` | `Guid` | Token ID (for revocation) |
| `iat` | `long` | Unix timestamp of issuance |
| `deviceId` | `string` | Device identifier |
| `accountId` | `Guid[]` | All accounts the user can access (multiple claims) |
| `signedInAccountId` | `Guid` | Currently active account |

## Login Metrics

Failed login attempts are tracked per user. After `MaxLoginAttempts` consecutive failures, the account is locked for `LockoutCooldownSeconds`.

| Option | Default | Description |
|--------|---------|-------------|
| `MaxLoginAttempts` | 5 | Consecutive failures before lockout |
| `LockoutCooldownSeconds` | 900 | Lockout duration (15 minutes) |
| `AuthTokenTtlSeconds` | 3600 | Token lifetime (1 hour) |

Configure in `appsettings.json` under the `SecurityOptions` section.

## Notes

- Tokens are signed with per-user asymmetric keys (not a shared secret)
- The system key encrypts stored private keys — it is never embedded in tokens
- `IUserContextSetter` is `internal` — only the host middleware and test infrastructure can set context directly
- If using `Corely.IAM.Web`, the `AuthenticationTokenMiddleware` handles token validation and context setup automatically
