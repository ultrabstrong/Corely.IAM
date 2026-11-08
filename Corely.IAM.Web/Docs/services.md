# Web Services

Services registered by `AddIAMWeb()` and `AddIAMWebBlazor()` for cookie management, Blazor authentication state, and UI synchronization.

## Service Table

| Interface | Implementation | Lifetime | Registered By |
|-----------|---------------|----------|--------------|
| `IAuthCookieManager` | `AuthCookieManager` | Singleton | `AddIAMWeb()` |
| `IUserContextClaimsBuilder` | `UserContextClaimsBuilder` | Singleton | `AddIAMWeb()` |
| `IBlazorUserContextAccessor` | `BlazorUserContextAccessor` | Scoped | `AddIAMWebBlazor()` |
| `AuthenticationStateProvider` | `IamAuthenticationStateProvider` | Scoped | `AddIAMWebBlazor()` |
| `IAccountDisplayState` | `AccountDisplayState` | Scoped | `AddIAMWebBlazor()` |
| `IOperationScopes` | `OperationScopes` | Scoped | `AddIAMWebBlazor()` |

## IAuthCookieManager

Manages authentication cookies (set, delete, device ID).

```csharp
public interface IAuthCookieManager
{
    void SetAuthCookies(IResponseCookies cookies, string authToken,
        Guid authTokenId, bool isHttps, int authTokenTtlSeconds);
    void DeleteAuthCookies(IResponseCookies cookies);
    void DeleteDeviceIdCookie(IResponseCookies cookies);
    string GetOrCreateDeviceId(HttpContext context);
}
```

- Auth cookies use HttpOnly, Secure and SameSite=Lax; see [Security](security.md#cookie-security) for why
- `GetOrCreateDeviceId()` creates a 90-day persistent cookie with a v7 UUID

## IBlazorUserContextAccessor

Cached user context accessor for Blazor components.

```csharp
public interface IBlazorUserContextAccessor
{
    Task<UserContext?> GetUserContextAsync();
}
```

- Fast path: returns context from `IUserContextProvider` if already set by middleware
- Slow path: reads `authentication_token` cookie, validates, and sets context
- Thread-safe via `SemaphoreSlim` with 5-second timeout
- Double-checked locking pattern to avoid redundant token validation

## IamAuthenticationStateProvider

Blazor `AuthenticationStateProvider` that derives auth state from `UserContext`.

- Returns an authenticated `ClaimsPrincipal` if user context is available
- Returns an anonymous principal otherwise
- Used by Blazor's `<CascadingAuthenticationState>` and `<AuthorizeView>`

## IUserContextClaimsBuilder

Maps `UserContext` to `ClaimsPrincipal` with standard + custom claims.

- Builds claims: `NameIdentifier` (user ID), `Name` (username), `Email`, `DeviceId`, `AccountId`, `SignedInAccountId`
- Sets authentication type to `"Cookies"` for ASP.NET Core compatibility

## IAccountDisplayState

Synchronizes account name display in the NavBar.

```csharp
public interface IAccountDisplayState
{
    string? AccountName { get; set; }
    event Action? OnChanged;
}
```

- Set by `AccountDetail` page when account name changes
- NavBar subscribes to `OnChanged` to update display

## IOperationScopes

Runs one user visible action of a Blazor component in a DI scope of its own, signed in with the tab's
auth token and disposed when the action ends, so every read sees the database as it is now.
`OperationComponentBase` and the page base classes wrap it as `InOperationAsync`.

```csharp
public interface IOperationScopes
{
    bool HasSession { get; }
    Task<T?> RunAsync<T>(Func<IServiceProvider, Task<T>> work);
    Task<bool> RunAsync(Func<IServiceProvider, Task> work);
}
```

- The token is read once per tab, from the request that opened it: a circuit's `HttpContext` is fixed
  when it connects.
- Without a session (`HasSession` false) no work runs and nothing navigates; the caller decides
  whether the visitor must sign in.
- When the token no longer signs in (expired or revoked), no work runs and the current page reloads,
  which renews the session from the refresh token or signs the user out.
