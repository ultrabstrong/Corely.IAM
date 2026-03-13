# User Context

Host-agnostic authentication context with no HttpContext dependency.

## UserContext Record

```csharp
public record UserContext(
    User User,
    Account? CurrentAccount,
    string DeviceId,
    List<Account> AvailableAccounts);
```

| Property | Description |
|----------|-------------|
| `User` | Authenticated user |
| `CurrentAccount` | Active account (null = no account selected) |
| `DeviceId` | Device identifier from token |
| `AvailableAccounts` | All accounts the user can access |

## IUserContextProvider (Read)

```csharp
public interface IUserContextProvider
{
    UserContext? GetUserContext();
    Task<UserAuthTokenValidationResultCode> SetUserContextAsync(string authToken);
}
```

- `GetUserContext()` — returns the current context, or `null` if not authenticated
- `SetUserContextAsync(token)` — validates a JWT token, extracts claims, and stores the context

## IUserContextSetter (Write)

```csharp
internal interface IUserContextSetter
{
    void SetUserContext(UserContext context);
    void ClearUserContext(Guid userId);
}
```

`IUserContextSetter` is `internal` — only host middleware and test infrastructure use it directly.

## Flow

1. Host authenticates the user (e.g., validates a cookie, extracts a JWT)
2. Host calls `IUserContextProvider.SetUserContextAsync(token)` to validate and set context
3. IAM services read context via `IUserContextProvider.GetUserContext()` for authorization decisions

If using `Corely.IAM.Web`, the `AuthenticationTokenMiddleware` handles steps 1-2 automatically.

## Host-Agnostic Design

`UserContextProvider` is registered as scoped — one instance per request. It has no dependency on `HttpContext`, `ClaimsPrincipal`, or any web framework type. This allows Corely.IAM to work in console apps, background services, or any .NET host.

## Token Validation Result Codes

| Code | Meaning |
|------|---------|
| `Success` | Token valid, context set |
| `InvalidTokenError` | JWT format invalid |
| `TokenNotFoundError` | Token not in database |
| `TokenRevokedError` | Token has been revoked |
| `TokenExpiredError` | Token has expired |
| `UserNotFoundError` | User from token not found |
| `MissingDeviceIdClaim` | Device ID claim missing |
| `SignatureValidationError` | JWT signature invalid |
