# Session hardening

**Status: done, not yet released.** Merged to master in Corely.IAM 6.0.0, Corely.IAM.Web 8.0.0 and
`corely-iam-db` 6.0.0, which ship together with [api-keys.md](api-keys.md).

## As built

Where the build differs from the plan below:

- **The refresh token follows the password recovery token.** It is `<token id>.<secret>`, found by
  the row id and checked against a salted hash (`IamHashCodes.TokenHashCode`) in
  `RefreshSecretHash`, not looked up by an unsalted SHA-256. No hash index is needed.
- **The rotated code is `RefreshTokenAlreadyRotatedError`**, matching the other failure codes.
- **Renewal claims the old row atomically.** A conditional `ExecuteUpdateAsync` sets `RevokedUtc` and
  `ReplacedByTokenId` only while both are null, so two renewals racing on one token cannot both
  succeed; the loser gets `RefreshTokenAlreadyRotatedError`.
- **The services revoke, not the processors.** The eight operations call
  `RevokeAllUserAuthTokensAsync(userId, exceptTokenId)` with the current token after a successful
  change; the services hold the user context the processors do not see.
- **The idle timeout may exceed the session lifetime.** The validator checks only
  `AuthTokenTtlSeconds <= AuthSessionIdleTimeoutSeconds`; the refresh expiry is the earlier of the
  two limits anyway, and an ordering rule would have refused existing short session configurations.
- **All three auth cookies expire with the refresh token**, path `/`, since the middleware renews on
  any request rather than at one endpoint.
- **Session queries moved to the refresh expiry.** Listing, sign out and every revocation select rows
  by `RefreshExpiresUtc`, so a session whose access token has lapsed can still be listed and revoked.
  `UserSession.ExpiresUtc` is the refresh expiry.
- **The migration backfills existing rows** with `RefreshExpiresUtc = ExpiresUtc`,
  `SessionStartedUtc = IssuedUtc` and `SessionId = Id`, so signing out still revokes a pre-upgrade
  token during its last hour, and sets the two new settings to their defaults on an existing settings
  row. Pre-upgrade sessions cannot renew.
- **A session start read from the database is normalized to UTC** before tokens are issued. Its
  unspecified `DateTimeKind` made the JWT library treat it as local time and shift a renewed token's
  expiry by the machine's offset; the functional clamp test caught it.
- **Switching account continues the session.** A security review after the first push found that
  `SwitchAccountAsync` started a fresh session, so a stolen access token could be traded for a new
  seven day session and refresh token. `GetUserAuthTokenRequest.ContinuedTokenId` now carries the
  current token; the switch claims it like a renewal (spent, `ReplacedByTokenId` set) and keeps its
  `SessionId` and `SessionStartedUtc`. A token whose session ended returns `SessionNotFoundError`.
- **The telemetry decorator logs no tokens.** It stopped logging the renew request, and the results
  of sign in, switch, MFA verification, Google sign in and renewal, all of which carry tokens.
- **An edge case audit after the first push** found that a pre-upgrade row (empty
  `RefreshSecretHash`) made a crafted refresh token throw instead of being refused; it is refused now.
  It added boundary tests (each expiry and the grace edge to the second), policy changes mid-session,
  credential changes and account switches on SQLite, and on SQL Server and MySQL a renewal round
  trip, the idle boundary, cleanup, and eight parallel renewals of one token, of which exactly one
  wins.
- **The Blazor Server unknown is unchanged by this work.** A circuit still holds its context for the
  connection and renews only on page requests, as before.

## What

Sign in sessions get a refresh token of their own, an idle timeout, revocation when a credential
changes, and a cleanup operation for old token rows. The new settings join the platform security
settings.

## Why

- **The access token renews itself.** `RenewUserAuthTokenAsync` validates the presented access token
  with lifetime checks off, so a stolen access token lives for the whole session (7 days by default),
  not the access token lifetime (1 hour).
- **Reuse is not detected.** When an attacker renews first, the real user's renewal fails and nothing
  else happens.
- **Nothing ends an idle session** before the absolute session lifetime.
- **Changing a credential leaves other sessions alive.** Password recovery revokes every token, but
  `ModifyPasswordAsync`, `RegisterPasswordAsync`, `DeregisterBasicAuthAsync`, the TOTP operations and
  Google link and unlink revoke none.
- **Tracked token rows are never deleted.** The table grows forever.

## Refresh tokens

Every sign in path returns an access token and a refresh token: password, MFA verification, Google,
account switching, and the API key exchange from [api-keys.md](api-keys.md).

- The refresh token is 32 random bytes, base64url encoded. Only its SHA-256 hash is stored; it is high
  entropy, so a salted slow hash adds nothing.
- It lives on the tracked token row. The row gains `RefreshTokenHash`, `RefreshExpiresUtc`,
  `SessionId`, `SessionStartedUtc` and `ReplacedByTokenId`.
- `SessionId` is the family. Sign in starts one; renewal keeps it.
- `RenewAuthTokenRequest` carries the refresh token instead of the access token. The access token is
  never accepted for renewal.
- Renewal looks the row up by hash, issues a new row in the same session, sets the old row's
  `RevokedUtc` and `ReplacedByTokenId`, and returns both new tokens.
- **Reuse detection:** a refresh token whose row was replaced revokes every row in its session and
  returns `RefreshTokenReusedError`. A row revoked any other way (sign out, credential change) returns
  `TokenValidationFailed` as today.
- **Concurrent renewal:** two requests renewing the same refresh token at once would look like reuse
  and sign the user out. A replaced token presented within `RefreshReuseGraceSeconds` of its
  replacement returns `RefreshTokenAlreadyRotated` without revoking the session, and the Web
  middleware leaves the cookies alone on that code; the browser already holds the new pair from the
  first response.
- `session_started_at` leaves the JWT; the row holds it. The access token keeps `sub`, `jti`,
  `device_id`, `account_id` and `signed_in_account_id`.

## Idle timeout

Each renewal sets `RefreshExpiresUtc` to the earlier of `now + AuthSessionIdleTimeoutSeconds` and
`SessionStartedUtc + AuthSessionTtlSeconds`. A refresh token past it fails with `SessionExpiredError`.
Sign in sets it the same way.

## Revocation on credential change

These revoke every other session of the user and keep the current one:

1. `ModifyPasswordAsync`
2. `RegisterPasswordAsync`
3. `DeregisterBasicAuthAsync`
4. `ConfirmTotpAsync` (TOTP turned on)
5. `DisableTotpAsync`
6. `RegenerateTotpRecoveryCodesAsync`
7. `LinkGoogleAuthAsync`
8. `UnlinkGoogleAuthAsync`

Always on, no setting. Password recovery keeps revoking every session, the current one included, since
there is no current one.

The processors call `RevokeOtherUserAuthTokensAsync(userId, currentTokenId)` after a successful change.
System context has no current token and is already refused by these operations.

## Token cleanup

`IAuthenticationService.DeleteExpiredAuthTokensAsync()` deletes rows whose `RefreshExpiresUtc` has
passed. Revoked rows stay until then, because reuse detection needs a replaced row to exist for as long
as its refresh token could be presented. One `ExecuteDeleteAsync`, so no rows are loaded.

It needs system context, as `DeleteExpiredEntriesAsync` does. IAM ships the operation and no runner:
no hosted service in `Corely.IAM` or `Corely.IAM.Web`. `Docs/authentication.md` gets a section showing
a host scheduling it with a `PeriodicTimer`, matching the audit cleanup section in `Docs/auditing.md`.

## Settings

New fields on `SecurityPolicy` and `PlatformSecuritySettingsEntity`, with range constants, validator
rules, `ChangesTo` entries and `SecurityOptions` configuration keys. `PlatformSecuritySettingsMapperTests`
fails until the entity has them.

| Setting | Default | Min | Max | Weakens when | Configuration key |
|---------|---------|-----|-----|--------------|-------------------|
| Idle timeout | 1 day | 5 minutes | 90 days | raised | `SecurityOptions:AuthSessionIdleTimeoutSeconds` |
| Refresh reuse grace | 30 seconds | 0 | 2 minutes | raised | `SecurityOptions:RefreshReuseGraceSeconds` |

Validator: `AuthTokenTtlSeconds <= AuthSessionIdleTimeoutSeconds <= AuthSessionTtlSeconds`. The
security settings page shows both fields with their ranges.

## Corely.IAM.Web

- `AuthCookieManager` sets a second cookie, `refresh`: HttpOnly, Secure, `SameSite=Lax` like the auth
  cookie, lifetime `RefreshExpiresUtc`. The access cookie's lifetime becomes the access token's.
- `AuthenticationTokenMiddleware` renews with the refresh cookie when the access token is missing or
  fails validation, and handles `RefreshTokenAlreadyRotated` as above.
- Sign out deletes both cookies.
- DevTools `auth renew` takes the refresh token.

## Tests

| Tier | Cases |
|------|-------|
| Unit | Refresh hash lookup; rotation sets `ReplacedByTokenId`; reuse revokes the session; reuse within grace does not; idle and absolute expiry each pick the earlier time; each of the eight operations revokes others and keeps current; validator ordering and ranges; `ChangesTo` weakening |
| Integration | Session wide revocation with `ExecuteUpdateAsync`; `DeleteExpiredAuthTokensAsync` deletes only rows past `RefreshExpiresUtc`; migration on both providers |
| Functional | Expired access cookie renews from the refresh cookie; concurrent renewal keeps the user signed in; reused refresh cookie signs out; idle session signs out after the fake clock passes the timeout |

## Release

Breaking: `RenewAuthTokenRequest` changes shape, sign in results gain a refresh token, and the JWT loses
`session_started_at`. Existing sessions end on upgrade, since their rows have no refresh hash.

- `Corely.IAM` 6.0.0, `Corely.IAM.Web` 8.0.0, `Corely.IAM.DataAccessMigrations.Cli` 6.0.0 (its major
  follows IAM's).
- Migration through `.\AddMigration.ps1 "SessionHardening"`.
- Ships in the same release as [api-keys.md](api-keys.md), since nothing is released while a plan is
  outstanding.

## Unknowns

- Whether `RefreshTokenAlreadyRotated` alone covers Blazor Server. A circuit holds its context for the
  whole connection and renews only on page requests; the functional concurrency test will show whether
  anything else is needed.
