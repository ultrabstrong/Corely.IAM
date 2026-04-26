# Password Recovery Flow

## Problem

`Corely.IAM` supports authenticated password changes through `SetPasswordAsync`, but it does not provide a forgot-password / recovery-token flow. For a library that already supports direct user sign-up and simple hosted-app usage, this is a meaningful missing surface.

## Scope

1. Add a host-agnostic password recovery flow that does not assume ASP.NET, email transport, or a specific UI.
2. Introduce explicit request / result models for initiating and completing recovery.
3. Define how recovery tokens are created, stored, expired, invalidated, and consumed.
4. Preserve the current separation between authenticated password change and unauthenticated password recovery.

## Approach

1. Audit the current basic-auth, user, and invitation patterns to choose the closest fit for password recovery tokens.
2. Decide whether recovery should use a dedicated domain entity or reuse an existing token-like pattern.
3. Design public service methods that let the host:
   - request recovery for a user identity
   - validate/consume a recovery token
   - set a new password
4. Keep delivery out of scope for the library itself; the host should own notification/email dispatch.
5. Add result codes and tests for success, invalid token, expired token, reused token, and unknown user handling.

## Notes

- Avoid leaking whether a username/email exists unless that is an intentional design decision.
- Keep this host-agnostic; email sending should not become a dependency of `Corely.IAM`.
- Reuse existing security patterns for hashing/encryption/token expiry where practical.

## Status

- Plan created.
- No implementation started.
