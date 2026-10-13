# Middleware

`UseIAMWebAuthentication()` registers four middleware components in a specific order. It sets no
security headers; see [Security headers](security.md#security-headers).

## Pipeline Order

```
1. CorrelationIdMiddleware
2. AuthenticationTokenMiddleware
3. UseAuthentication()
4. UseAuthorization()
```

Order matters: each middleware builds on the previous.

## CorrelationIdMiddleware

Assigns a correlation ID for request tracing.

- Reads `X-Correlation-ID` from the request header
- If absent, generates a new v7 UUID
- Sets the header on the response
- Pushes the ID into Serilog's `LogContext` for structured logging

## AuthenticationTokenMiddleware

Validates JWT cookies and sets user context.

1. Reads `authentication_token` cookie from the request
2. Calls `IAuthenticationService.AuthenticateWithTokenAsync(token)` to validate
3. On success: builds `ClaimsPrincipal` via `IUserContextClaimsBuilder` and sets `HttpContext.User`
4. On failure: clears all auth cookies via `IAuthCookieManager.DeleteAuthCookies()`

Note: Both `IAuthenticationService` and `IUserContextProvider` are scoped. They must be resolved from the request scope, not constructor-injected.

## Configuration

The middleware pipeline is registered as a single call:

```csharp
app.UseIAMWebAuthentication();
```

Place this before `UseHttpsRedirection()` and before the endpoints, `MapStaticAssets()` included, are
mapped.
