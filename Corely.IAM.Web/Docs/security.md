# Security Features

Security headers and cookie protection provided out of the box by `UseIAMWebAuthentication()`. The
Content-Security-Policy is not among them; the host sets its own.

## Cookie Security

All authentication cookies use:

| Flag | Value |
|------|-------|
| `HttpOnly` | `true`: not accessible from JavaScript |
| `Secure` | `true` (when HTTPS): only sent over encrypted connections |
| `SameSite` | `Strict`: not sent with cross-site requests |
| `Path` | `/`: available site-wide |

## Auth Cookies

| Cookie | Purpose | Expiry |
|--------|---------|--------|
| `authentication_token` | JWT token | `AuthSessionTtlSeconds` (default: 7 days); the JWT inside still uses `AuthTokenTtlSeconds` for access-token expiry |
| `auth_token_id` | Token ID for revocation | Same as `authentication_token` |
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

`Strict-Transport-Security: max-age=31536000; includeSubDomains`, enabled in non-development environments only.

## Content Security Policy

This package sets no Content-Security-Policy. A policy has to list every source a page loads
(analytics, a CDN, Google sign-in), and only the host knows those. Set one in the host:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; "
        + "connect-src 'self' wss: ws:; img-src 'self' data:; font-src 'self'; "
        + "frame-ancestors 'none'; form-action 'self'; base-uri 'self'; object-src 'none'";
    await next();
});
app.UseIAMWebAuthentication();
```

That is a working starting point for this package's pages on Blazor Server:

- `'unsafe-inline'`: Blazor Server's startup and reconnection scripts are inline, and Bootstrap
  sets inline styles.
- `wss:` and `ws:`: Blazor Server talks to the server over a SignalR WebSocket.

### Google sign-in

With `SecurityOptions:GoogleClientId` set, the sign-in, register, and profile pages load Google
Identity Services, which renders its button in an iframe. Add Google's sources to the directives
above:

| Directive | Add |
|-----------|-----|
| `script-src` | `https://accounts.google.com/gsi/client` |
| `frame-src` | `https://accounts.google.com/gsi/` |
| `connect-src` | `https://accounts.google.com/gsi/` |
| `style-src` | `https://accounts.google.com/gsi/style` |

Without them the browser blocks the script and the Google button never appears.
