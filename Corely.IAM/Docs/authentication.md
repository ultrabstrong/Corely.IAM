# Authentication

JWT-based authentication with custom claims, device tracking, and multi-account support. No HttpContext dependency — works in any .NET host.

## Features

- **JWT tokens** — signed with per-user asymmetric keys, validated on every request
- **Custom claims** — `account_id`, `signed_in_account_id`, `device_id` embedded in token
- **Account switching** — issue new tokens scoped to a different account without re-authenticating
- **Device tracking** — tokens bound to device IDs for session management
- **Session management** — list active sessions, revoke one session, or revoke all other sessions
- **Login metrics** — failed attempt counting and lockout cooldown
- **Bulk sign-out** — revoke all tokens for a user across all devices
- **Password recovery** — email-based token flow for unauthenticated password reset
- **MFA (TOTP)** — optional second factor via authenticator apps (see [mfa.md](mfa.md))
- **Google Sign-In** — alternative auth method via Google ID tokens (see [google-signin.md](google-signin.md))

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
await authenticationService.SignOutAsync(new SignOutRequest(tokenId.ToString()));

// All sessions for the current user
await authenticationService.SignOutAllAsync();
```

`SignOutAsync` revokes the specific token. `SignOutAllAsync` revokes all active tokens for the user across all devices.

## Session Management

Tracked auth tokens are the backing store for user sessions. Each active `UserAuthTokenEntity` represents one active session for a `(user, signed-in account, device)` combination.

```csharp
var sessions = await authenticationService.ListSessionsAsync();

await authenticationService.RevokeSessionAsync(new RevokeSessionRequest(sessionId));
await authenticationService.RevokeOtherSessionsAsync();
```

`ListSessionsAsync()` returns the current user's active sessions only — revoked and expired tokens are excluded. Each session includes its tracked token ID, device ID, signed-in account ID, issued/expiry timestamps, and whether it is the current session.

## Password Recovery

Forgot-password flows are handled by `IPasswordRecoveryService`, not `IAuthenticationService`:

```csharp
var requestResult = await passwordRecoveryService.RequestPasswordRecoveryAsync(
    new RequestPasswordRecoveryRequest("user@example.com"));
```

The host app delivers the returned token through its own trusted channel. A successful reset updates or creates the user's basic-auth credential, revokes all active auth tokens, and clears lockout state. See [services/password-recovery.md](services/password-recovery.md) for the full flow.

## Token Validation

Set user context by validating a token through `IAuthenticationService`:

```csharp
var authService = serviceProvider.GetRequiredService<IAuthenticationService>();
var result = await authService.AuthenticateWithTokenAsync(token);

if (result == UserAuthTokenValidationResultCode.Success)
{
    var context = userContextProvider.GetUserContext();
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
- `IUserContextSetter` is `internal` — only `AuthenticationService` and test infrastructure can set context directly
- If using `Corely.IAM.Web`, the `AuthenticationTokenMiddleware` handles token validation and context setup automatically

## System Context (Headless Processes)

For background services, Azure Functions, or other headless processes that need to call IAM services without authenticating as a user, use `IAuthenticationService.AuthenticateAsSystem()`:

```csharp
var authService = serviceProvider.GetRequiredService<IAuthenticationService>();
authService.AuthenticateAsSystem("my-function-app");

// All IAM service calls in this scope now bypass permission checks
var result = await retrievalService.ListUsersAsync(request);
```

System context is fully permissioned but blocks "self" operations (MFA, password management, Google auth) that only make sense for a logged-in user. See [User Context](security/user-context.md) and [Authorization](authorization.md) for details.
