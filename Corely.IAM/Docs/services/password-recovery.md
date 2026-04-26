# IPasswordRecoveryService

Manages unauthenticated password-recovery requests, token validation, and password reset. See [Authentication](../authentication.md) for the surrounding auth flow.

## Methods

| Method | Parameters | Returns |
|--------|-----------|---------|
| `RequestPasswordRecoveryAsync` | `RequestPasswordRecoveryRequest` | `RequestPasswordRecoveryResult` |
| `ValidatePasswordRecoveryTokenAsync` | `ValidatePasswordRecoveryTokenRequest` | `ValidatePasswordRecoveryTokenResult` |
| `ResetPasswordWithRecoveryAsync` | `ResetPasswordWithRecoveryRequest` | `ResetPasswordWithRecoveryResult` |

## Usage

### Request Recovery

```csharp
var result = await passwordRecoveryService.RequestPasswordRecoveryAsync(
    new RequestPasswordRecoveryRequest("user@example.com"));

if (
    result.ResultCode == RequestPasswordRecoveryResultCode.Success
    && result.RecoveryToken != null
)
{
    // Host app delivers the token through email, SMS, etc.
}
```

If no user matches the email, the result returns `UserNotFoundError` and no token. The host decides whether to expose or suppress that distinction to end users.

### Validate Recovery Token

```csharp
var result = await passwordRecoveryService.ValidatePasswordRecoveryTokenAsync(
    new ValidatePasswordRecoveryTokenRequest(token));
```

Validation distinguishes between not found, expired, already used, and invalidated recovery tokens.

### Reset Password

```csharp
var result = await passwordRecoveryService.ResetPasswordWithRecoveryAsync(
    new ResetPasswordWithRecoveryRequest(token, "N3wP@ssword!"));
```

On success, the password is updated or created, all active auth tokens for the user are revoked, and login lockout state is cleared.

## Result Codes

### Request

| Code | Meaning |
|------|---------|
| `Success` | Recovery record created and token returned |
| `UserNotFoundError` | No user matched the supplied email |
| `ValidationError` | Email format invalid |

### Validate

| Code | Meaning |
|------|---------|
| `Success` | Token is valid and still pending |
| `PasswordRecoveryNotFoundError` | Token malformed or record not found |
| `PasswordRecoveryExpiredError` | Token expired |
| `PasswordRecoveryAlreadyUsedError` | Token already consumed |
| `PasswordRecoveryInvalidatedError` | A newer request replaced this token |

### Reset

| Code | Meaning |
|------|---------|
| `Success` | Password reset completed |
| `PasswordRecoveryNotFoundError` | Token malformed or record not found |
| `PasswordRecoveryExpiredError` | Token expired |
| `PasswordRecoveryAlreadyUsedError` | Token already consumed |
| `PasswordRecoveryInvalidatedError` | A newer request replaced this token |
| `PasswordValidationError` | New password failed password rules |
| `ValidationError` | Request failed model validation |

## Authorization

- No authenticated user context is required
- Only the telemetry decorator is applied
- The host is responsible for delivering the returned token through a trusted channel
