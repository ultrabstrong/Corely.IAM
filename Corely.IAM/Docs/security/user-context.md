# User Context

Host-agnostic authentication context with no HttpContext dependency. Supports both user-authenticated and system (headless) contexts.

## UserContext Record

```csharp
public record UserContext
{
    public User? User { get; init; }
    public Account? CurrentAccount { get; init; }
    public string DeviceId { get; init; }
    public List<Account> AvailableAccounts { get; init; }
    public bool IsSystemContext { get; init; }
}
```

| Property | Description |
|----------|-------------|
| `User` | Authenticated user (null for system context) |
| `CurrentAccount` | Active account (null = no account selected) |
| `DeviceId` | Device identifier from token (identifies the calling host for system context) |
| `AvailableAccounts` | All accounts the user can access (empty for system context) |
| `IsSystemContext` | `true` when context was set via `IAuthenticationService.AuthenticateAsSystem()` |

## IUserContextProvider (Read-Only)

```csharp
public interface IUserContextProvider
{
    UserContext? GetUserContext();
}
```

- `GetUserContext()` — returns the current context, or `null` if not authenticated

`IUserContextProvider` is read-only. All context-setting goes through `IAuthenticationService`.

## IAuthenticationService (Context Setting)

All context-setting is routed through `IAuthenticationService`, which is the single public entry point for establishing authentication context:

```csharp
public interface IAuthenticationService
{
    // ... sign-in, sign-out, MFA methods ...
    Task<UserAuthTokenValidationResultCode> AuthenticateWithTokenAsync(string authToken);
    void AuthenticateAsSystem(string deviceId);
}
```

- `AuthenticateWithTokenAsync(token)` — validates a JWT token, extracts claims, and stores the user context
- `AuthenticateAsSystem(deviceId)` — creates a fully-permissioned system context for headless processes

### System Context Usage

```csharp
var authService = serviceProvider.GetRequiredService<IAuthenticationService>();
authService.AuthenticateAsSystem("my-function-app");

// Now all IAM service calls in this scope pass authorization checks
var users = await retrievalService.ListUsersAsync(request);
```

### System Context Behavior

- System context passes `HasUserContext()`, `HasAccountContext()`, and `IsAuthorizedAsync()` checks
- System context is blocked by `IsNonSystemUserContext()` — "self" operations (MFA, password, Google auth) require a real user
- System context has no `User`, `CurrentAccount`, or `AvailableAccounts` — services that extract user identity from context will fail if called from system context

## IUserContextSetter (Internal)

```csharp
internal interface IUserContextSetter
{
    void SetUserContext(UserContext context);
    void SetSystemContext(string deviceId);
    void ClearUserContext(Guid userId);
}
```

`IUserContextSetter` is `internal` — only `AuthenticationService` and test infrastructure use it directly.

## Flow

### User Authentication Flow

1. Host authenticates the user (e.g., validates a cookie, extracts a JWT)
2. Host calls `IAuthenticationService.AuthenticateWithTokenAsync(token)` to validate and set context
3. IAM services read context via `IUserContextProvider.GetUserContext()` for authorization decisions

If using `Corely.IAM.Web`, the `AuthenticationTokenMiddleware` handles steps 1-2 automatically.

### System Context Flow

1. Background process resolves `IAuthenticationService` from DI
2. Calls `AuthenticateAsSystem(deviceId)` to establish a fully-permissioned context
3. IAM services detect system context and bypass permission checks

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
