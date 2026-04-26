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
- Item 5 is deferred for now. Revisit it only if there is a cleaner path that does not require injecting `IamDbContext` directly into the auth-provider flow.

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
- Item 5 reopened:
  - The current token-revocation batching implementation was not accepted and has been removed from the staged set.
  - Follow-up on item 5 is deferred until there is a better option that does not require injecting `IamDbContext` directly.
  - `PasswordRecoveryProcessor.InvalidatePendingRecoveriesAsync()` uses the same list-and-update-each-row pattern as auth-token bulk revocation; it is a lower-priority path, but it should be treated as another candidate if a cleaner repo-level bulk update capability is introduced later.
  - Items 1-4 remain complete; this plan stays in progress until item 5 is either redesigned or dropped.
