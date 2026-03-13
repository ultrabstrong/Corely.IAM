# Authentication Flow

Pre-authentication pages are Razor Pages (server-rendered, no Blazor dependency). After authentication, users are redirected to Blazor Server management pages.

## Pre-Authentication Pages

| Route | Page | Purpose |
|-------|------|---------|
| `/signin` | SignIn | Login form (username + password) |
| `/register` | Register | Registration form (username, email, password, confirm) |
| `/select-account` | SelectAccount | Account selection with search and pagination |
| `/create-account` | CreateAccount | New account form |
| `/signout` | SignOut | Sign out and clear cookies |

## Sign-In Flow

1. User submits username and password at `/signin`
2. `IAuthenticationService.SignInAsync()` validates credentials
3. On success: `AuthCookieManager.SetAuthCookies()` stores HttpOnly cookies
4. If single account: auto-switch to that account and redirect to dashboard
5. If multiple accounts: redirect to `/select-account`
6. If no accounts: redirect to dashboard (user can create an account)

## Registration Flow

1. User submits username, email, password, and confirmation at `/register`
2. `IRegistrationService.RegisterUserAsync()` creates the user
3. On success: auto sign-in via `IAuthenticationService.SignInAsync()`
4. Redirect to dashboard
5. If auto sign-in fails: redirect to `/signin`

## Account Switching

1. User selects an account on `/select-account`
2. `IAuthenticationService.SwitchAccountAsync()` issues a new token scoped to the selected account
3. `AuthCookieManager.SetAuthCookies()` replaces the existing cookies
4. Redirect to dashboard

The select-account page supports search by account name and pagination (10 per page).

## Cookie Details

| Cookie | Purpose | Flags |
|--------|---------|-------|
| `auth_token` | JWT token | HttpOnly, Secure, SameSite=Strict, TTL-based expiry |
| `auth_token_id` | Token ID for revocation | HttpOnly, Secure, SameSite=Strict, TTL-based expiry |
| `device_id` | Device fingerprint | HttpOnly, Secure, SameSite=Strict, 90-day expiry |

All auth cookies use `Path=/`. The `device_id` cookie is created on first visit (sign-in or register) and persists across sessions.

## Token Validation Middleware

`AuthenticationTokenMiddleware` runs on every request:

1. Reads `auth_token` cookie
2. Calls `IUserContextProvider.SetUserContextAsync(token)` to validate the JWT
3. On success: builds `ClaimsPrincipal` via `IUserContextClaimsBuilder` and sets `HttpContext.User`
4. On failure: clears all auth cookies

This happens before ASP.NET Core's `UseAuthentication()` and `UseAuthorization()` middleware.

## Notes

- Pre-auth pages use the `_AuthLayout` layout (centered card with tab navigation between Sign In and Sign Up)
- The `auth_token` cookie TTL comes from `SecurityOptions.AuthTokenTtlSeconds` (default: 3600)
- If a user navigates to `/signin` while already authenticated, they are redirected to the dashboard
- `GetOrCreateDeviceId()` creates a new device ID cookie if one doesn't exist, using a v7 UUID
