# Token Refresh and Renewal

## Problem

Authentication currently relies on fixed-lifetime JWTs with no refresh-token or renewal path. For a web-facing IAM library, consumers may reasonably expect a first-class story for maintaining sessions without forcing a full sign-in every time the access token expires.

## Scope

1. Evaluate refresh-token versus renewal/sliding-session approaches in the context of the current JWT + tracked-token model.
2. Prefer a design that stays host-agnostic and does not require web-only assumptions.
3. Preserve explicit revocation semantics and device-aware token tracking.
4. Clarify interaction with MFA, Google sign-in, account switching, and sign-out.

## Approach

1. Audit the current auth-token issuance, validation, revocation, and MFA challenge flow.
2. Compare feasible renewal designs:
   - refresh token pair
   - tracked renewable session with rotated access tokens
   - bounded sliding expiration
3. Choose the design that best fits the existing security model and implementation constraints.
4. Define public request/result models and result codes for renewal.
5. Add tests for rotation, expiry, replay/reuse protection, revocation interaction, and multi-device behavior.

## Notes

- This should be designed intentionally with session management, not bolted onto raw JWT issuance.
- Avoid a design that quietly weakens current revocation guarantees.
- Coordinate with the deferred token-batching work only if it becomes necessary; do not make that a prerequisite for planning this surface.

## Status

- Implemented as a host-agnostic, tracked-token renewal flow with bounded sessions.
- Added `AuthSessionTtlSeconds` to bound the renewable session lifetime independently from the short-lived access-token TTL.
- Added `RenewAuthTokenAsync` to `IAuthenticationService` plus public renewal request/result models and result codes.
- Tokens now carry a `session_started_at` claim so renewals can rotate JWTs while preserving the original session lifetime.
- `AuthenticationTokenMiddleware` now attempts renewal before clearing auth cookies, and Web auth cookies now live for the bounded session window so renewal works after idle periods.
- Added focused provider, service, middleware, and post-authentication tests plus DevTools `auth renew` support.
