# Web Auth and Performance Improvements

## Problem

The latest review of `Corely.IAM` and `Corely.IAM.Web` found a short list of real improvements worth doing. These are not feature ideas or style cleanups; they are targeted fixes for duplicated behavior, caching behavior, robustness, auth-state cleanup, and token-revocation efficiency.

## Scope

1. **Extract duplicated post-authentication account-switch flow**
   - Current duplicate logic exists in:
     - `Corely.IAM.Web\Pages\Authentication\SignIn.cshtml.cs`
     - `Corely.IAM.Web\Pages\Authentication\VerifyMfa.cshtml.cs`
     - `Corely.IAM.Web\Pages\Authentication\GoogleCallback.cshtml.cs`
     - `Corely.IAM.Web\Pages\Authentication\RegisterWithGoogle.cshtml.cs`
   - Goal: centralize cookie-setting + account auto-switch + redirect behavior in a shared web-layer service/helper.

2. **Fix static asset caching behavior**
   - `SecurityHeadersMiddleware` currently sets non-cacheable headers for all responses.
   - Goal: preserve strict caching rules for dynamic/auth responses while avoiding unnecessary no-store behavior for static assets.

3. **Harden Blazor user-context acquisition**
   - `BlazorUserContextAccessor` currently returns `null` after a short semaphore timeout.
   - Goal: avoid spurious auth loss under contention by removing or relaxing that failure mode.

4. **Clean auth failure cookies consistently**
   - `AuthenticationTokenMiddleware` clears auth cookies on token validation failure, but leaves the device-id cookie behind.
   - Goal: make auth-state cleanup consistent.

5. **Improve token revocation efficiency**
   - `AuthenticationProvider` revokes collections of tokens with one awaited update per token.
   - Goal: reduce avoidable database round trips when revoking multiple tokens.

## Implementation Order

1. Extract duplicated post-authentication flow
2. Fix static asset caching behavior
3. Harden Blazor user-context acquisition
4. Clean auth failure cookies consistently
5. Improve token revocation efficiency

## Notes

- Keep the current user-facing behavior intact unless the improvement explicitly changes it.
- Favor shared abstractions only where they remove real duplication or harden correctness.
- Each item should include test coverage or updated validation around the changed behavior.
- Item 5 was deferred until a cleaner path existed than injecting `IamDbContext` directly into the auth-provider flow. That condition was later met by a repo-level bulk update capability.

## Status

- Plan created.
- Item 1 completed:
  - Added `IPostAuthenticationFlowService` / `PostAuthenticationFlowService` in `Corely.IAM.Web\Services\`.
  - Updated the four authentication page models to delegate post-auth cookie/account-switch redirect handling to the shared service.
  - Added focused `Corely.IAM.Web.UnitTests` coverage for the shared service and updated the affected page-model tests to verify delegation.
- Item 2 completed:
  - `SecurityHeadersMiddleware` now keeps dynamic/auth responses non-cacheable while allowing cacheable static-asset requests to use `public, max-age=86400`.
  - Added focused middleware tests covering both dynamic and static-asset cache-header behavior.
- Item 3 completed:
  - `BlazorUserContextAccessor` no longer returns `null` because a short semaphore wait timed out while another caller was already authenticating.
  - Added a focused concurrency test proving concurrent callers share a single authentication attempt and both receive the populated context.
- Item 4 completed:
  - `AuthenticationTokenMiddleware` now clears the device-id cookie alongside auth cookies when token validation fails or throws.
  - Updated middleware tests to assert that both auth-token cleanup and device-id cleanup happen on failure paths.
- Item 5 completed:
  - `Corely.DataAccess` 2.3.0 adds `IRepo.ExecuteUpdateAsync(query, setProperties)`, exposing EF's
    set-based update through the repository seam. This is the "cleaner repo-level bulk update
    capability" the earlier deferral was waiting on - no `IamDbContext` injection required.
  - `AuthenticationProvider.RevokeOtherUserAuthTokensAsync`, `RevokeAllUserAuthTokensAsync`, and
    `RevokeExistingTokensForUserAccountDeviceAsync` now issue a single UPDATE instead of one
    awaited update per token.
  - `PasswordRecoveryProcessor.InvalidatePendingRecoveriesAsync()` - flagged here as the sibling
    candidate - was converted to the same pattern.
  - The efficiency gain is modest, since these paths touch few rows. The material win is
    atomicity: revocation previously saved each token in its own transaction, so a mid-loop
    failure could leave a user partially signed out after a password reset.
  - `MockRepo` satisfies `ExecuteUpdateAsync` by interpreting the `SetProperty` expression tree in
    memory. `Corely.DataAccess` covers that with parity tests asserting identical behavior against
    both the mock and real EF on SQLite.
- All five items are complete; this plan is done.

## Testing follow-ups

Recorded here so they are not lost. Tracked in `Plans/New/testing-strategy.md`.

- The IAM revocation predicates are only exercised through `MockRepo`, since every IAM test runs
  on the mock database. EF's translation of them - notably the nullable `t.AccountId == accountId`
  comparison - is unverified in this repo. A throwaway-LocalDB fixture was prototyped, confirmed
  the translation works, and was then removed deliberately rather than bolted onto the unit test
  project.
- Token renewal has no end-to-end verification. Nobody has signed into the WebApp, let an access
  token expire, and confirmed the middleware renews it in a real browser. This is the one flow
  that depends on wall-clock time and cookie lifetime, which unit tests cannot meaningfully
  assert - and it is exactly where the cookie-lifetime bug would have hidden.
- Renewal deliberately does not re-challenge MFA. A session that passed MFA at sign-in stays
  renewable for the full `AuthSessionTtlSeconds` window. This was reviewed and accepted.
