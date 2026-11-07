# Authentication Flow

Pre-authentication pages are Razor Pages (server-rendered, no Blazor dependency). After authentication, users are redirected to the host's pages.

## Pre-Authentication Pages

| Route | Page | Purpose |
|-------|------|---------|
| `/signin` | SignIn | Login form (username + password) |
| `/register` | Register | Registration form (username, email, password, confirm) |
| `/select-account` | SelectAccount | Account selection with search and pagination |
| `/create-account` | CreateAccount | New account form |
| `/signout` | SignOut | Sign out and clear cookies; asks first unless the request is same origin |
| `/verify-mfa` | VerifyMfa | MFA code entry after sign-in |
| `/signin/start/{provider}` | FederatedSignInStart | Sends the browser to an identity provider |
| `/signin/callback/{provider}` | FederatedSignInCallback | Completes the sign in the provider sends back |

## Sign-In Flow

1. User submits username and password at `/signin`
2. `IAuthenticationService.SignInAsync()` validates credentials
3. On success: `AuthCookieManager.SetAuthCookies()` stores HttpOnly cookies
4. If single account: auto-switch to that account and redirect to the return URL, or `IAMWebOptions.HomePath`
5. If multiple accounts: redirect to the return URL (its page's `AccountScope` switches into the right account), or `/select-account`
6. If no accounts: redirect to `HomePath` (user can create an account)

## Registration Flow

1. User submits username, email, password, and confirmation at `/register`
2. `IRegistrationService.RegisterUserAsync()` creates the user
3. On success: auto sign-in via `IAuthenticationService.SignInAsync()`
4. Redirect to `HomePath`
5. If auto sign-in fails: redirect to `/signin`

## MFA Verification Flow

When TOTP is enabled for a user, sign-in (password or identity provider) returns `MfaRequiredChallenge` instead of a JWT:

1. Sign-in succeeds at credential level, but `SignInResult.ResultCode == MfaRequiredChallenge`
2. The MFA challenge token is stored in `TempData` and the user is redirected to `/verify-mfa`
3. User enters a 6-digit TOTP code or a recovery code (`XXXX-XXXX` format)
4. `IAuthenticationService.VerifyMfaAsync()` validates the code
5. On success: cookies are set and the user proceeds as in the sign-in flow, return URL included
6. On expired challenge: redirect back to `/signin`
7. On invalid code: error message displayed, user can retry

## Identity Provider Flow

Each enabled provider gets a "Continue with" button on `/signin`, and on `/register` when it allows
sign up. The button is a plain link, so the page never loads the provider's script. See
[Federated Sign-In](../../Corely.IAM/Docs/federated-sign-in.md) for the rules.

1. `/signin/start/{provider}` (GET) calls `IFederatedSignInService.StartFederatedSignInAsync()`,
   seals the correlation (`state`, PKCE verifier, nonce, issuer, purpose, return URL) in the
   `__Host-federated_sign_in` cookie with ASP.NET Core Data Protection, and redirects to the provider
2. The provider redirects back to `/signin/callback/{provider}` with `code` and `state`
3. The callback takes the cookie (deleting it), checks it names the same provider, and calls
   `CompleteFederatedSignInAsync()`
4. Signed in, or signed up and signed in: cookies are set and the user proceeds as in the sign-in
   flow, return URL included. With TOTP on: `/verify-mfa`
5. Anything refused: an error page with a way back, and no cookie changes

The same pages link and prove it is you: the profile's Linked Accounts section starts
`?purpose=link`, and the key panel starts `?purpose=reauthenticate`, each returning to its own page.

The callback is the one GET that changes state when another site started it, as OAuth requires of a
redirection endpoint. It acts only on a response that matches the correlation cookie set by this
browser's own start, and the cookie is `SameSite=Lax` so it rides the provider's redirect back.

A host running several instances must share the Data Protection key ring between them, as
antiforgery already requires.

## Account Switching

1. User selects an account on `/select-account`
2. `IAuthenticationService.SwitchAccountAsync()` issues a new token scoped to the selected account
3. `AuthCookieManager.SetAuthCookies()` replaces the existing cookies
4. Redirect to its `returnUrl` when local, otherwise `HomePath`

The select-account page supports search by account name and pagination (10 per page). A member of the [platform account](../../Corely.IAM/Docs/platform.md) holding Account Read there sees every account, and can switch into one they don't belong to.

## Return URLs

A signed out visitor to an `[Authorize]` page is sent to `/signin?ReturnUrl=<the page>` by the
cookie middleware, and `RedirectToLogin`, `AuthenticatedPageBase` and `AccountScope` send
`?returnUrl=` the same way. Sign in lands back on that page, carrying it through every step:

| Step | Carried by |
|------|------------|
| `/signin` | The query string (either spelling), then a hidden field in the form |
| `/verify-mfa` | `TempData`, beside the MFA challenge, then a hidden field |
| Identity provider sign in | Sealed in the `__Host-federated_sign_in` cookie with the rest of the correlation |
| `/select-account` | `?returnUrl=` and a hidden field, through to the switch |
| `/switch-account` | `?returnUrl=`, kept on the confirm form when another site sent the request |

Every step keeps a return URL only when it is a path on this site (`/accounts/…`); anything else,
`https://…`, `//host` and `/\host` included, is dropped and the flow goes on as if there were
none. An open redirect here would be a phishing tool.

## Cookie Details

| Cookie | Purpose | Flags |
|--------|---------|-------|
| `authentication_token` | JWT access token | HttpOnly, Secure, SameSite=Lax, expires with the session |
| `auth_token_id` | Token ID for revocation | HttpOnly, Secure, SameSite=Lax, expires with the session |
| `refresh_token` | Refresh token for renewal | HttpOnly, Secure, SameSite=Lax, expires with the session |
| `device_id` | Device fingerprint | HttpOnly, Secure, SameSite=Strict, 90-day expiry |
| `__Host-federated_sign_in` | Correlation for one identity provider sign in, encrypted | HttpOnly, Secure, SameSite=Lax, 10 minutes, deleted when read |

All auth cookies use `Path=/`. The `device_id` cookie is created on first visit (sign-in or register) and persists across sessions.

## Token Validation Middleware

`AuthenticationTokenMiddleware` runs on every request:

1. Reads the `authentication_token` and `refresh_token` cookies
2. Calls `IAuthenticationService.AuthenticateWithTokenAsync(token)` to validate the JWT
3. If the access token is missing or fails, calls `IAuthenticationService.RenewAuthTokenAsync(...)` with the refresh token and writes the new pair to the cookies
4. On success: builds `ClaimsPrincipal` via `IUserContextClaimsBuilder` and sets `HttpContext.User`
5. On `RefreshTokenAlreadyRotatedError` (another request renewed the same token a moment earlier): leaves the cookies alone and serves the request unauthenticated
6. On any other failure: clears all auth cookies

This happens before ASP.NET Core's `UseAuthentication()` and `UseAuthorization()` middleware.

## Notes

- Pre-auth pages use the `_AuthLayout` layout (centered card with tab navigation between Sign In and Sign Up)
- The auth cookies expire when the refresh token does: the idle timeout or the session lifetime, whichever comes first. Each renewal moves them
- If a user navigates to `/signin` while already authenticated, they are redirected to the return URL, or `HomePath`
- `GetOrCreateDeviceId()` creates a new device ID cookie if one doesn't exist, using a v7 UUID

## Busy state on form posts

The authentication pages are Razor Pages form posts, not Blazor components, so there is no circuit
to bind a busy flag to. `_AuthLayout` loads `_content/Corely.IAM.Web/js/form-busy.js`, which on
submit of any `method="post"` form disables its submit buttons and sets `aria-busy`. This prevents a
second post of the same credentials while the first is in flight.

Buttons marked `data-busy-spinner` also swap their label for a spinner after 150 ms:

```html
<button type="submit" data-busy-spinner class="btn btn-primary w-100">Sign In</button>
```

The delay means an action that completes quickly shows nothing rather than flashing a spinner. The
button's width is pinned before the swap so the card does not resize.

Buttons without the attribute are disabled but keep their label. That is correct where the label is content
rather than a verb, as on the account list in Select Account. `method="get"` forms are left alone.

Progressive enhancement: with scripting unavailable the forms still post normally.
