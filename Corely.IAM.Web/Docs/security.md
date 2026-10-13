# Security Features

Cookie protection comes with `UseIAMWebAuthentication()`. Security headers, the
Content-Security-Policy among them, are the host's: this package sets none.

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
| `authentication_token` | JWT token | The session lifetime (default: 7 days); the JWT inside still uses the token lifetime for access-token expiry. Both come from `ISecurityPolicyProvider` |
| `auth_token_id` | Token ID for revocation | Same as `authentication_token` |
| `device_id` | Device fingerprint | 90 days |

## Security headers

Which headers suit an app depends on the app: whether it embeds its own pages, which browser
features it needs, how it caches. So this package sets none. A starting set, before
`UseIAMWebAuthentication()`:

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XFrameOptions = "DENY";
    headers.XContentTypeOptions = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    headers["Cross-Origin-Opener-Policy"] = "same-origin";
    headers["Cross-Origin-Resource-Policy"] = "same-origin";
    headers["X-Permitted-Cross-Domain-Policies"] = "none";
    context.Response.OnStarting(() =>
    {
        if (headers.CacheControl.Count == 0)
        {
            headers.CacheControl = "no-store";
        }
        return Task.CompletedTask;
    });
    headers.ContentSecurityPolicy =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; "
        + "connect-src 'self' wss: ws:; img-src 'self' data:; font-src 'self'; "
        + "frame-ancestors 'none'; form-action 'self'; base-uri 'self'; object-src 'none'";
    await next();
});
app.UseIAMWebAuthentication();
```

`no-store` goes only on a response that has no `Cache-Control` of its own, so pages are never
cached while `MapStaticAssets` keeps caching static files.

## Content Security Policy

A policy has to list every source a page loads (analytics, a CDN, Google sign-in), and only the
host knows those. The one above is a working starting point for this package's pages on Blazor
Server:

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
