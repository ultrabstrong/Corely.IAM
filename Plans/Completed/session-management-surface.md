# Session Management Surface

## Problem

The library tracks auth tokens and device IDs and documents this as supporting session management, but the public API only exposes sign-out of the current token and sign-out of all tokens. It does not expose a real session-management surface such as listing active sessions or revoking specific sessions.

## Scope

1. Define what a "session" means in this library relative to `UserAuthTokenEntity`, device ID, account context, and token expiry.
2. Add a host-agnostic public surface for viewing and revoking sessions without requiring direct repository access.
3. Keep the design compatible with the current token-tracking model and user-context rules.
4. Avoid introducing admin-over-other-user session management unless there is an explicit product reason.

## Approach

1. Audit the current auth-token tracking entity and sign-out paths.
2. Design a session model suitable for public retrieval, likely derived from tracked auth tokens rather than raw JWTs.
3. Design service methods for:
   - listing active sessions for the current user
   - revoking a selected session
   - optionally revoking all other sessions while preserving the current session
4. Decide how device ID, issued time, expiry time, and signed-in account should be represented.
5. Add tests covering current-session revocation, other-session revocation, expired-token exclusion, and multi-device behavior.

## Notes

- This should align with the existing "users manage themselves, not other users" philosophy.
- The session model should be safe for direct host/UI consumption and should not expose raw token secrets.
- Reuse existing revocation logic instead of duplicating auth-token behavior in a second abstraction.

## Status

- Completed.
- `IAuthenticationService` now exposes `ListSessionsAsync()`, `RevokeSessionAsync(...)`, and `RevokeOtherSessionsAsync()` for current-user session management.
- Sessions are now modeled as active tracked auth tokens, surfaced as `UserSession` records with session ID, device ID, signed-in account ID, issued/expiry timestamps, and current-session flag.
- `UserContext` now carries the tracked auth-token ID so the current session can be identified and preserved when revoking other sessions.
- Added focused provider/service/telemetry tests covering session listing, selected-session revocation, and revoke-other-sessions behavior.
- Authentication and user-context docs were updated, and the full repository rebuild/test output is clean.
