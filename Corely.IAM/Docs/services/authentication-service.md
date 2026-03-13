# IAuthenticationService

Manages sign-in, sign-out, and account switching. See [Authentication](../authentication.md) for the full flow and token details.

## Methods

| Method | Parameters | Returns |
|--------|-----------|---------|
| `SignInAsync` | `SignInRequest` | `SignInResult` |
| `SwitchAccountAsync` | `SwitchAccountRequest` | `SignInResult` |
| `SignOutAsync` | `SignOutRequest` | `bool` |
| `SignOutAllAsync` | *(none)* | `void` |

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
    new SignOutRequest(tokenId, deviceId));

// All sessions
await authenticationService.SignOutAllAsync();
```

## Result Codes

| Code | Meaning |
|------|---------|
| `Success` | Authentication succeeded |
| `UserNotFoundError` | Username not found |
| `InvalidPasswordError` | Password incorrect |
| `AccountLockedError` | Too many failed attempts |
| `AccountNotFoundError` | Target account not found (switch) |
| `AuthorizationError` | Not authorized |

## Authorization

- `SignInAsync` does not require prior authentication
- `SwitchAccountAsync` requires user context
- `SignOutAsync` and `SignOutAllAsync` require user context

## Notes

- `SignInAsync` does not require prior authentication — it is the entry point
- Only the telemetry decorator is applied (no authorization decorator) since sign-in must work without existing context
- Login failure counting and lockout are handled at the processor level
