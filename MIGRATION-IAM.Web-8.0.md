# Migrating to Corely.IAM.Web 8.0

Corely.IAM.Web 8.0 targets Corely.IAM 6.0 (see [MIGRATION-6.0.md](MIGRATION-6.0.md)), whose sessions
renew with a refresh token. The auth cookies carry it; a host that only uses IAM's pages and
middleware has nothing to change for that. The reconnect overlay's markup changed too.

## Cookies

A third cookie, `refresh_token`, joins `authentication_token` and `auth_token_id`, with the same
attributes (HttpOnly, Secure over HTTPS, `SameSite=Lax`). All three expire when the session can no
longer be renewed: the idle timeout or the session lifetime, whichever comes first.

`AuthenticationTokenMiddleware` renews from the refresh cookie when the access token is missing or
invalid. When another request renewed the same token a moment earlier, it leaves the cookies alone
rather than signing the user out.

## Host code that sets the auth cookies

`IAuthCookieManager.SetAuthCookies` takes the refresh token and its expiry instead of a lifetime in
seconds:

```csharp
// 7.x
cookieManager.SetAuthCookies(cookies, result.AuthToken!, result.AuthTokenId!.Value, isHttps, ttlSeconds);

// 8.0
cookieManager.SetAuthCookies(
    cookies,
    result.AuthToken!,
    result.AuthTokenId!.Value,
    result.RefreshToken!,
    result.RefreshExpiresUtc!.Value,
    isHttps);
```

`DeleteAuthCookies` deletes the refresh cookie too.

## Reconnect overlay

The overlay styles moved from `#components-reconnect-modal` to classes, and show a card on a dimmed
page. Give the element `class="components-reconnect-modal"` and the card markup in
[styling.md](Corely.IAM.Web/Docs/styling.md); without it the overlay is unstyled. Add
`_content/Corely.IAM.Web/js/reconnect-reload.js` to reload on its own when the circuit is rejected.

## Security settings page

The Sign in section gains the idle timeout and the refresh reuse grace.
