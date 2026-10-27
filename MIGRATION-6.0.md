# Migrating to Corely.IAM 6.0

Sign in now returns a refresh token beside the access token, and only the refresh token renews a
session. Sessions also end after an idle timeout, other sessions end when a credential changes, and
expired token rows can be deleted. One migration adds the columns. `corely-iam-db` 6.0 goes with it.

## 1. Apply the migration

```powershell
corely-iam-db db migrate -p MsSql -c "your-connection-string"
```

It adds the two new settings to `PlatformSecuritySettings` and renames `UserAuthTokens` to
`AuthTokens`, one table for the sessions of users and API keys. A session that existed before the
upgrade cannot be renewed, so its user signs in again once its access token expires.

## 2. Keep the refresh token

`SignInResult` gains `RefreshToken` and `RefreshExpiresUtc`. Store the refresh token wherever the
host kept the access token; a browser host keeps it in an HttpOnly cookie that lives until
`RefreshExpiresUtc`. `Corely.IAM.Web` does this for you.

## 3. Renew with the refresh token

```csharp
// 5.x
await authenticationService.RenewAuthTokenAsync(new RenewAuthTokenRequest(accessToken));

// 6.0
var renewed = await authenticationService.RenewAuthTokenAsync(
    new RenewAuthTokenRequest(refreshToken));
// store renewed.AuthToken and renewed.RefreshToken; the old refresh token is spent
```

An access token passed to `RenewAuthTokenAsync` is refused. Every renewal returns a new refresh token
and spends the old one.

## 4. Handle the new result codes

| Code | Means | Do |
|------|-------|----|
| `RefreshTokenReusedError` | A spent refresh token came back after the grace window; the whole session was ended | Sign the user out |
| `RefreshTokenAlreadyRotatedError` | Another request renewed the same token moments ago | Keep the stored tokens; the other request holds the new pair |

`RenewAuthTokenResultCode.InvalidTokenFormat`, `MissingUserIdClaim` and `MissingDeviceIdClaim` are
gone; a malformed refresh token returns `InvalidAuthTokenError`.

## 5. Schedule the cleanup

Token rows were never deleted. `IAuthenticationService.DeleteExpiredAuthTokensAsync()` deletes the
ones past their refresh expiry. It needs system context, and IAM ships no runner; see
[authentication.md](Corely.IAM/Docs/authentication.md#deleting-expired-tokens).

## 6. Smaller changes

| Change | Instead |
|--------|---------|
| The JWT's `session_started_at` claim is gone | The tracked token row holds the session start |
| Sessions stayed open until `AuthSessionTtlSeconds` | They also end after `AuthSessionIdleTimeoutSeconds` unused (default 1 day) |
| Changing a password, setting or removing one, turning TOTP on or off, regenerating recovery codes, and linking or unlinking Google left other sessions alive | They end every other session of the user |
| `SecurityPolicy` had 12 settings | It gains `AuthSessionIdleTimeoutSeconds` and `RefreshReuseGraceSeconds`; a `SecurityPolicy` built positionally needs both |
| `SwitchAccountAsync` started a new session | It continues the current one and spends its refresh token; the result carries the new pair |
| The telemetry decorator logged sign in, switch, MFA, Google and renew results at Trace | It logs that they ran, not their results, which carry tokens |
| `UserSession.ExpiresUtc` was the access token's expiry | It is the refresh token's expiry, when the session ends if left alone |
| `SecurityPolicy` had no API key settings | It gains `ApiKeysEnabled`, `PlatformAccountApiKeysEnabled`, `ApiKeyMaxLifetimeDays` and `ApiKeyResourceTypes`; keys stay off until turned on. See [API keys](Corely.IAM/Docs/authentication.md#api-keys) |
| `UserContext.User` was always set outside system context | It is null when an API key is signed in; `UserContext.ApiKey` is set instead |
| `AuditActions`, `AuditCohort` and `AuditDetail` were in `Corely.IAM.Audits.Models`; `InvitationStatus` was in `Corely.IAM.Invitations.Models` | They are in `Corely.IAM.Audits.Enums` and `Corely.IAM.Invitations.Enums` |
| Deleting a group, role or permission, and changing a group's members or roles, a role's permissions or a user's roles, looked the row up by id alone | The row must belong to the account the request names; one in another account is not found |
| `BootstrapPlatformAsync` ran for any caller while no platform account existed | It runs only with no signed-in caller (the migration CLI) or in system context; anyone else gets `BootstrapPlatformResultCode.UnauthorizedError` |
| In system context, a request naming `Guid.Empty` as its account matched rows in any account | It matches nothing; pass the real account. A system caller with no current account still reads across accounts through `IRetrievalService` |
| `RegisterUserWithAccountAsync` let anyone with Update on an account add any existing user to it | It runs only in system context. A user joins an account by creating it or by accepting an invitation; a host that seeds accounts calls it after `AuthenticateAsSystem` |
