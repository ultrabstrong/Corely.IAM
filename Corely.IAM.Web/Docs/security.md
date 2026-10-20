# Security Features

Cookie protection comes with `UseIAMWebAuthentication()`. Security headers, the
Content-Security-Policy among them, are the host's: this package sets none.

## Cookie Security

All authentication cookies use:

| Flag | Value |
|------|-------|
| `HttpOnly` | `true`: not accessible from JavaScript |
| `Secure` | `true` (when HTTPS): only sent over encrypted connections |
| `SameSite` | `Lax` on `authentication_token` and `auth_token_id`, `Strict` on `device_id` |
| `Path` | `/`: available site-wide |

`Lax` sends the auth cookies when someone follows a link to the site from elsewhere, an email
included, so a deep link opens signed in. It never sends them on another site's POST, or on a
request a page makes in the background, the Blazor circuit's WebSocket among them. Two rules keep it
safe, and are tested:

1. **Every POST carries an antiforgery token.** SameSite is defence in depth, not the CSRF defence.
2. **No GET changes state when another site started it.** `/signout` and `/switch-account` act on
   GET only when the browser's `Sec-Fetch-Site` header says `same-origin`, which is how a host's own
   links and `AccountScope` reach them. From anywhere else, or with no header, each shows a
   confirm button that posts with an antiforgery token. `CrossSiteRequestTests` sends a cross site
   GET to every endpoint in `AppRoutes` and fails if any changes the session.
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

## Human check

The sign up page asks an `IHumanVerifier` before it creates a user. The default passes everything, so
a host that adds nothing sees no change. To put a bot check (Cloudflare Turnstile, hCaptcha,
reCAPTCHA) in front of sign up and your own forms, the host brings the provider:

1. **Implement `IHumanVerifier`** and register it. Registered before or after `AddIAMWeb`, it
   replaces the default.

   ```csharp
   builder.Services.AddScoped<IHumanVerifier, TurnstileVerifier>();
   ```

   `VerifyAsync(httpContext, action, cancellationToken)` reads the provider's token from the posted
   form and returns `Success`, `MissingToken`, `Rejected` or `Unavailable`. `action` names the form:
   `"register"` for sign up.
2. **Override the widget slot.** The sign up form renders `Pages/Authentication/_HumanCheck.cshtml`,
   empty in this package, above its submit button, with the action name as its model. Add a file at
   that path in the host to render the provider's widget:

   ```cshtml
   @model string
   <div class="cf-turnstile" data-sitekey="..." data-action="@Model" data-theme="auto"></div>
   <script src="https://challenges.cloudflare.com/turnstile/v0/api.js" async defer></script>
   ```
3. **Check your own forms.** A forgot password page sends an email per request, so check it too:
   call the verifier with your own action name, show `result.RefusalMessage` when it does not
   succeed, and render the same partial. `Corely.IAM.WebApp`'s `ForgotPassword` page does this.
4. **Allow the provider in your content security policy.** Turnstile needs
   `https://challenges.cloudflare.com` in `script-src` and `frame-src`.

What a verifier must check:

- **The provider's verdict on the server.** The widget alone proves nothing.
- **The hostname and action the provider reports,** so a token solved on another site or another
  form is `Rejected`. Behind a proxy the request's `Host` can be an internal name, so take the
  hostnames to accept from configuration.
- **Fail closed.** A timeout, an error status or an unreadable reply is `Unavailable`, and the page
  refuses the post. Let request cancellation propagate.
- **Never log the token or the provider secret.**
- **The client IP, if you send it, from forwarded headers.** Behind a proxy `RemoteIpAddress` is the
  proxy's.

The widget sends visitor data to the provider; say so in your privacy policy. A human check does
not replace rate limiting: add `AddRateLimiter` on sign in, sign up and forgot password.
