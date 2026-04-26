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

- Plan created.
- No implementation started.
