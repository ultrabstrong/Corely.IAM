# Middleware

`UseIAMWebAuthentication()` registers five middleware components in a specific order.

## Pipeline Order

```
1. CorrelationIdMiddleware
2. SecurityHeadersMiddleware
3. AuthenticationTokenMiddleware
4. UseAuthentication()
5. UseAuthorization()
```

Order matters — each middleware builds on the previous.

## CorrelationIdMiddleware

Assigns a correlation ID for request tracing.

- Reads `X-Correlation-ID` from the request header
- If absent, generates a new v7 UUID
- Sets the header on the response
- Pushes the ID into Serilog's `LogContext` for structured logging

## SecurityHeadersMiddleware

Adds security response headers on every request.

| Header | Value |
|--------|-------|
| `X-Frame-Options` | `DENY` |
| `X-Content-Type-Options` | `nosniff` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=()` |
| `Cache-Control` | `no-store, no-cache, must-revalidate` |
| `Cross-Origin-Opener-Policy` | `same-origin` |
| `Cross-Origin-Resource-Policy` | `same-origin` |
| `X-Permitted-Cross-Domain-Policies` | `none` |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` (non-development only) |

It does not set a `Content-Security-Policy`; the host owns that. See
[Content Security Policy](security.md#content-security-policy) for a starting policy.

## AuthenticationTokenMiddleware

Validates JWT cookies and sets user context.

1. Reads `authentication_token` cookie from the request
2. Calls `IAuthenticationService.AuthenticateWithTokenAsync(token)` to validate
3. On success: builds `ClaimsPrincipal` via `IUserContextClaimsBuilder` and sets `HttpContext.User`
4. On failure: clears all auth cookies via `IAuthCookieManager.DeleteAuthCookies()`

Note: Both `IAuthenticationService` and `IUserContextProvider` are scoped — they must be resolved from the request scope, not constructor-injected.

## Configuration

The middleware pipeline is registered as a single call:

```csharp
app.UseIAMWebAuthentication();
```

Place this before `UseHttpsRedirection()` and `UseStaticFiles()` in the middleware pipeline.
