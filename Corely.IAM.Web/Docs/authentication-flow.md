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
| `/verify-mfa` | VerifyMfa | MFA code entry after sign-in |
| `/google-callback` | GoogleCallback | Google Identity Services callback |

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

## MFA Verification Flow

When TOTP is enabled for a user, sign-in (both password and Google) returns `MfaRequiredChallenge` instead of a JWT:

1. Sign-in succeeds at credential level, but `SignInResult.ResultCode == MfaRequiredChallenge`
2. The MFA challenge token is stored in `TempData` and the user is redirected to `/verify-mfa`
3. User enters a 6-digit TOTP code or a recovery code (`XXXX-XXXX` format)
4. `IAuthenticationService.VerifyMfaAsync()` validates the code
5. On success: cookies are set and the user proceeds to account selection/dashboard
6. On expired challenge: redirect back to `/signin`
7. On invalid code: error message displayed, user can retry

## Google Sign-In Flow

When `GoogleClientId` is configured in `SecurityOptions`, a "Sign in with Google" button appears on the sign-in page:

1. Google Identity Services library loaded on `/signin`
2. User clicks the Google button and authenticates with Google
3. Google POSTs the ID token to `/google-callback`
4. `IAuthenticationService.SignInWithGoogleAsync()` validates the token and finds the linked user
5. If TOTP is enabled: redirects to `/verify-mfa` (same as password flow)
6. On success: cookies are set and the user proceeds to account selection/dashboard
7. If no user is linked: redirects to `/register-with-google` (see below)

## Google Sign-Up Flow

When a user signs in with Google but has no linked account, they are redirected to the registration prompt:

1. `/google-callback` receives `GoogleAuthNotLinkedError` from `SignInWithGoogleAsync`
2. Stores the Google ID token in `TempData` and redirects to `/register-with-google`
3. User sees "No account found — create one?" confirmation page
4. On confirm: `IRegistrationService.RegisterUserWithGoogleAsync()` creates the user (username auto-generated from email)
5. Auto-signs in via `SignInWithGoogleAsync` → cookies set → dashboard
6. The same Google button on `/register` follows the same flow via `/google-callback`

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
2. Calls `IAuthenticationService.AuthenticateWithTokenAsync(token)` to validate the JWT
3. On success: builds `ClaimsPrincipal` via `IUserContextClaimsBuilder` and sets `HttpContext.User`
4. On failure: clears all auth cookies

This happens before ASP.NET Core's `UseAuthentication()` and `UseAuthorization()` middleware.

## Notes

- Pre-auth pages use the `_AuthLayout` layout (centered card with tab navigation between Sign In and Sign Up)
- The `auth_token` cookie TTL comes from `SecurityOptions.AuthTokenTtlSeconds` (default: 3600)
- If a user navigates to `/signin` while already authenticated, they are redirected to the dashboard
- `GetOrCreateDeviceId()` creates a new device ID cookie if one doesn't exist, using a v7 UUID
