# Authentication

JWT-based authentication with custom claims, device tracking, and multi-account support. No HttpContext dependency, so it works in any .NET host.

## Features

- **JWT tokens**: signed with per-user asymmetric keys, validated on every request
- **Custom claims**: `account_id`, `signed_in_account_id`, `device_id` embedded in token
- **Account switching**: issue new tokens scoped to a different account without re-authenticating
- **Device tracking**: tokens bound to device IDs for session management
- **Refresh tokens**: renew a short lived access token with a single use refresh token; a reused one ends its session
- **Session management**: list active sessions, revoke one session, or revoke all other sessions
- **Login metrics**: failed attempt counting and lockout cooldown
- **Bulk sign-out**: revoke all tokens for a user across all devices
- **Password recovery**: email-based token flow for unauthenticated password reset
- **MFA (TOTP)**: optional second factor via authenticator apps (see [mfa.md](mfa.md))
- **Google Sign-In**: alternative auth method via Google ID tokens (see [google-signin.md](google-signin.md))

## Sign-In Flow

1. User submits username + password to `IAuthenticationService.SignInAsync()`
2. `BasicAuthProcessor` validates credentials against hashed password
3. On success, `AuthenticationProvider` generates a JWT signed with the user's private key
4. Token includes claims: `sub` (user ID), `jti` (token ID), `iat`, `deviceId`, `accountId` (one per available account)
5. A tracking record is created in the database for revocation support, holding the hash of a new refresh token
6. Returns `SignInResult` with the access token, its ID, the refresh token and when the refresh token expires

```csharp
var result = await authenticationService.SignInAsync(
    new SignInRequest("jdoe", "P@ssw0rd!", deviceId));

if (result.ResultCode == SignInResultCode.Success)
{
    var token = result.AuthToken;
    var tokenId = result.AuthTokenId;
    var refreshToken = result.RefreshToken;
}
```

## Renewal

The access token lives `AuthTokenTtlSeconds`. Renew it with the refresh token, never with the access
token:

```csharp
var renewed = await authenticationService.RenewAuthTokenAsync(
    new RenewAuthTokenRequest(refreshToken));

if (renewed.ResultCode == RenewAuthTokenResultCode.Success)
{
    accessToken = renewed.AuthToken;
    refreshToken = renewed.RefreshToken;
}
```

Each renewal issues a new access token and a new refresh token in the same session, and spends the
old refresh token. The session ends at the first of:

- `AuthSessionTtlSeconds` after sign in, however often it renews
- `AuthSessionIdleTimeoutSeconds` after the last renewal
- a sign out, a revocation, or a credential change on another session

A spent refresh token presented again ends the whole session and returns `RefreshTokenReusedError`:
only a copy taken by someone else can be presented twice. Within `RefreshReuseGraceSeconds` of being
spent it returns `RefreshTokenAlreadyRotatedError` instead and leaves the session alone, since two
requests renewing at the same moment look the same.

The refresh token is `<token id>.<secret>`. Only a salted hash of the secret is stored.

## Account Switching

After sign-in, switch the active account without re-entering credentials:

```csharp
var result = await authenticationService.SwitchAccountAsync(
    new SwitchAccountRequest(targetAccountId));
```

This spends the current token and issues a new pair with `signedInAccountId` set to the target account, in the same session: the session start, its lifetime and its idle timeout carry over, and the spent refresh token counts as reused if it comes back. A token whose session has ended cannot switch.

## Sign-Out

```csharp
// Single session
await authenticationService.SignOutAsync(new SignOutRequest(tokenId.ToString()));

// All sessions for the current user
await authenticationService.SignOutAllAsync();
```

`SignOutAsync` revokes the specific token. `SignOutAllAsync` revokes all active tokens for the user across all devices.

Changing a credential ends every other session of the user and keeps the current one: changing,
setting or removing the password, turning TOTP on or off, regenerating recovery codes, and linking or
unlinking Google. A password reset through recovery ends every session.

## Session Management

Tracked auth tokens are the backing store for user sessions. Each active `UserAuthTokenEntity` represents one active session for a `(user, signed-in account, device)` combination.

```csharp
var sessions = await authenticationService.ListSessionsAsync();

await authenticationService.RevokeSessionAsync(new RevokeSessionRequest(sessionId));
await authenticationService.RevokeOtherSessionsAsync();
```

`ListSessionsAsync()` returns the current user's active sessions only; revoked and expired tokens are excluded. Each session includes its tracked token ID, device ID, signed-in account ID, when its current token was issued, when the session ends if left alone, and whether it is the current session.

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

## In an API

`Corely.IAM.Web` validates and renews from cookies on its own. An API host that takes bearer tokens
writes the same steps itself: a middleware that sets the context from the `Authorization` header, and
endpoints for sign in and renewal. The client keeps both tokens and renews when a call returns 401.

```csharp
app.Use(async (context, next) =>
{
    var header = context.Request.Headers.Authorization.ToString();
    if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        var result = await context.RequestServices
            .GetRequiredService<IAuthenticationService>()
            .AuthenticateWithTokenAsync(header["Bearer ".Length..]);
        if (result != UserAuthTokenValidationResultCode.Success)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
    }

    await next(context);
});

app.MapPost("/auth/signin", async (SignInRequest request, IAuthenticationService auth) =>
{
    var result = await auth.SignInAsync(request);
    return result.ResultCode == SignInResultCode.Success
        ? Results.Ok(new { result.AuthToken, result.RefreshToken, result.RefreshExpiresUtc })
        : Results.Unauthorized();
});

app.MapPost("/auth/renew", async (RenewAuthTokenRequest request, IAuthenticationService auth) =>
{
    var result = await auth.RenewAuthTokenAsync(request);
    return result.ResultCode switch
    {
        RenewAuthTokenResultCode.Success =>
            Results.Ok(new { result.AuthToken, result.RefreshToken, result.RefreshExpiresUtc }),
        RenewAuthTokenResultCode.RefreshTokenAlreadyRotatedError => Results.Conflict(),
        _ => Results.Unauthorized(),
    };
});
```

A request without a header passes through with no context, so IAM's own authorization refuses
anything that needs a user. On 409 from `/auth/renew` another call of the same client renewed first:
the client keeps the pair that call received. On 401 it signs in again.

## Token Claims

| Claim | Type | Description |
|-------|------|-------------|
| `sub` | `Guid` | User ID |
| `jti` | `Guid` | Token ID (for revocation) |
| `iat` | `long` | Unix timestamp of issuance |
| `device_id` | `string` | Device identifier |
| `account_id` | `Guid[]` | All accounts the user can access (multiple claims) |
| `signed_in_account_id` | `Guid` | Currently active account |

## Login Metrics

Failed login attempts are tracked per user. After `MaxLoginAttempts` consecutive failures, the account is locked for `LockoutCooldownSeconds`. A wrong current password given to change or remove a password counts as a failure too.

| Option | Default | Description |
|--------|---------|-------------|
| `MaxLoginAttempts` | 5 | Consecutive failures before lockout |
| `LockoutCooldownSeconds` | 900 | Lockout duration (15 minutes) |
| `AuthTokenTtlSeconds` | 3600 | Access token lifetime (1 hour) |
| `AuthSessionTtlSeconds` | 604800 | Maximum renewable session lifetime (7 days) |
| `AuthSessionIdleTimeoutSeconds` | 86400 | Session ends when not renewed for this long (1 day) |
| `RefreshReuseGraceSeconds` | 30 | How long a spent refresh token is refused without ending its session |

Configure in `appsettings.json` under the `SecurityOptions` section. With a platform account these come
from the platform's security settings instead; see
[where security settings come from](platform.md#where-security-settings-come-from).

## Notes

- Tokens are signed with per-user asymmetric keys (not a shared secret)
- Renewals rotate tracked tokens within one session, so the session start and its bound carry over
- The system key encrypts stored private keys. It is never embedded in tokens
- `IUserContextSetter` is `internal`. Only `AuthenticationService` and test infrastructure can set context directly
- If using `Corely.IAM.Web`, the `AuthenticationTokenMiddleware` handles token validation and context setup automatically

## Deleting Expired Tokens

Tracked token rows stay after they expire. `DeleteExpiredAuthTokensAsync` deletes every row past its
refresh expiry, in system context. IAM ships the operation; the host ships the schedule:

```csharp
using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
while (await timer.WaitForNextTickAsync(stoppingToken))
{
    using var scope = serviceProvider.CreateScope();
    scope.ServiceProvider.GetRequiredService<IAuthenticationService>().AuthenticateAsSystem("token-cleanup");
    var result = await scope.ServiceProvider
        .GetRequiredService<IAuthenticationService>()
        .DeleteExpiredAuthTokensAsync();
}
```

A replaced row is kept until its refresh expiry so that a reuse of it is still recognized.

## System Context (Headless Processes)

For background services, Azure Functions, or other headless processes that need to call IAM services without authenticating as a user, use `IAuthenticationService.AuthenticateAsSystem()`:

```csharp
var authService = serviceProvider.GetRequiredService<IAuthenticationService>();
authService.AuthenticateAsSystem("my-function-app");

// All IAM service calls in this scope now bypass permission checks
var result = await retrievalService.ListUsersAsync(request);
```

System context is fully permissioned but blocks "self" operations (MFA, password management, Google auth) that only make sense for a logged-in user. See [User Context](security/user-context.md) and [Authorization](authorization.md) for details.
