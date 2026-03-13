# Security Features

Security headers, cookie protection, and content security policy provided out of the box by `UseIAMWebAuthentication()`.

## Cookie Security

All authentication cookies use:

| Flag | Value |
|------|-------|
| `HttpOnly` | `true` — not accessible from JavaScript |
| `Secure` | `true` (when HTTPS) — only sent over encrypted connections |
| `SameSite` | `Strict` — not sent with cross-site requests |
| `Path` | `/` — available site-wide |

## Auth Cookies

| Cookie | Purpose | Expiry |
|--------|---------|--------|
| `auth_token` | JWT token | `AuthTokenTtlSeconds` (default: 1 hour) |
| `auth_token_id` | Token ID for revocation | Same as `auth_token` |
| `device_id` | Device fingerprint | 90 days |

## Security Headers

| Header | Value | Purpose |
|--------|-------|---------|
| `X-Frame-Options` | `DENY` | Prevents clickjacking |
| `X-Content-Type-Options` | `nosniff` | Prevents MIME sniffing |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Controls referrer information |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=()` | Restricts browser features |
| `Cache-Control` | `no-store, no-cache, must-revalidate` | Prevents response caching |
| `Cross-Origin-Opener-Policy` | `same-origin` | Isolates browsing context |
| `Cross-Origin-Resource-Policy` | `same-origin` | Restricts resource loading |
| `X-Permitted-Cross-Domain-Policies` | `none` | Blocks cross-domain policies |

## HSTS

`Strict-Transport-Security: max-age=31536000; includeSubDomains` — enabled in non-development environments only.

## Content Security Policy

```
default-src 'self';
script-src 'self' 'unsafe-inline';
style-src 'self' 'unsafe-inline';
connect-src 'self' wss: ws:;
img-src 'self' data:;
font-src 'self';
frame-ancestors 'none';
form-action 'self';
base-uri 'self';
object-src 'none'
```

### Why `unsafe-inline`?

Blazor Server requires inline scripts for initialization and reconnection. Bootstrap uses inline styles for dynamic components. Both require `'unsafe-inline'` in their respective directives.

### Why `wss:` and `ws:`?

Blazor Server communicates with the server via SignalR WebSocket connections. The `connect-src` directive must allow WebSocket protocols.

## CSP Customization

If the host app adds external scripts or styles (e.g., Google Fonts, analytics), override the CSP by registering a custom `SecurityHeadersMiddleware` or by adding headers after `UseIAMWebAuthentication()`:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] = "your-custom-csp";
    await next();
});
```

Note: This replaces the entire CSP header. There is no merge mechanism.
