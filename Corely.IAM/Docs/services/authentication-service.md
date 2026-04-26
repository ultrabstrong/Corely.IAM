# IAuthenticationService

Manages sign-in, sign-out, account switching, and user session management. See [Authentication](../authentication.md) for the full flow and token details.

## Methods

| Method | Parameters | Returns |
|--------|-----------|---------|
| `SignInAsync` | `SignInRequest` | `SignInResult` |
| `SwitchAccountAsync` | `SwitchAccountRequest` | `SignInResult` |
| `ListSessionsAsync` | *(none)* | `RetrieveListResult<UserSession>` |
| `RevokeSessionAsync` | `RevokeSessionRequest` | `ModifyResult` |
| `RevokeOtherSessionsAsync` | *(none)* | `ModifyResult` |
| `SignOutAsync` | `SignOutRequest` | `bool` |
| `SignOutAllAsync` | *(none)* | `void` |
| `SignInWithGoogleAsync` | `SignInWithGoogleRequest` | `SignInResult` |
| `VerifyMfaAsync` | `VerifyMfaRequest` | `SignInResult` |

## Usage

### Sign In

```csharp
var result = await authenticationService.SignInAsync(
    new SignInRequest("jdoe", "P@ssw0rd!", deviceId));

if (result.ResultCode == SignInResultCode.Success)
{
    var token = result.AuthToken;
    var tokenId = result.AuthTokenId;
}
```

### Switch Account

```csharp
var result = await authenticationService.SwitchAccountAsync(
    new SwitchAccountRequest(targetAccountId));
```

Issues a new token scoped to the target account. Revokes the previous token.

### Sign Out

```csharp
// Single session
var success = await authenticationService.SignOutAsync(
    new SignOutRequest(tokenId.ToString()));

// All sessions
await authenticationService.SignOutAllAsync();
```

### Session Management

```csharp
var sessions = await authenticationService.ListSessionsAsync();

var revokeResult = await authenticationService.RevokeSessionAsync(
    new RevokeSessionRequest(sessionId));

var revokeOthersResult = await authenticationService.RevokeOtherSessionsAsync();
```

`ListSessionsAsync()` returns active tracked sessions for the current user. `RevokeSessionAsync()` revokes a selected active session by tracked token ID. `RevokeOtherSessionsAsync()` revokes all other active sessions while preserving the current one.

### Sign In with Google

```csharp
var result = await authenticationService.SignInWithGoogleAsync(
    new SignInWithGoogleRequest(googleIdToken, deviceId));
```

Returns `Success` or `MfaRequiredChallenge` (if TOTP is enabled). See [Google Sign-In](../google-signin.md) for full flow.

### Verify MFA

Complete a two-phase sign-in when TOTP is enabled:

```csharp
var result = await authenticationService.VerifyMfaAsync(
    new VerifyMfaRequest(challengeToken, totpCode));
```

Accepts either a 6-digit TOTP code or a recovery code in `XXXX-XXXX` format. See [MFA](../mfa.md) for full flow.

## Result Codes

| Code | Meaning |
|------|---------|
| `Success` | Authentication succeeded |
| `UserNotFoundError` | Username not found |
| `InvalidPasswordError` | Password incorrect |
| `AccountLockedError` | Too many failed attempts |
| `AccountNotFoundError` | Target account not found (switch) |
| `AuthorizationError` | Not authorized |
| `MfaRequiredChallenge` | TOTP enabled — MFA challenge issued |
| `InvalidMfaCodeError` | TOTP or recovery code invalid |
| `MfaChallengeExpiredError` | Challenge expired or already used |
| `InvalidGoogleTokenError` | Google ID token validation failed |
| `GoogleAuthNotLinkedError` | No user linked to this Google account |

## Authorization

- `SignInAsync` does not require prior authentication
- `SwitchAccountAsync` requires user context
- `ListSessionsAsync`, `RevokeSessionAsync`, `RevokeOtherSessionsAsync`, `SignOutAsync`, and `SignOutAllAsync` require a non-system user context

## Notes

- `SignInAsync` does not require prior authentication — it is the entry point
- Only the telemetry decorator is applied (no authorization decorator) since sign-in must work without existing context
- Login failure counting and lockout are handled at the processor level
