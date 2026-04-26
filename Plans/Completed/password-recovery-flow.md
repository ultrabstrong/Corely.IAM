# Password Recovery Flow

## Problem

`Corely.IAM` supports authenticated password changes through `SetPasswordAsync`, but it does not provide a forgot-password / recovery-token flow. For a library that already supports direct user sign-up and simple hosted-app usage, this is a meaningful missing surface.

## Scope

1. Add a host-agnostic password recovery flow that does not assume ASP.NET, email transport, or a specific UI.
2. Introduce explicit request / result models for initiating and completing recovery.
3. Define how recovery tokens are created, stored, expired, invalidated, and consumed.
4. Preserve the current separation between authenticated password change and unauthenticated password recovery.

## Design Decisions

These decisions were explicitly chosen before fleshing out the rest of the design:

1. **Recovery identifier:** email only
2. **Unknown user behavior:** explicit user-not-found result for library callers
3. **Public token flow:** validate and consume are separate public operations
4. **Multiple requests:** newer requests invalidate older unused tokens
5. **Recovery token TTL:** fixed 10 minutes for the initial implementation
6. **Recovery token configuration:** no host-configurable TTL knob in the first version

## Proposed Public Surface

Introduce a dedicated service rather than folding this into `IRegistrationService` or `IAuthenticationService`:

```csharp
public interface IPasswordRecoveryService
{
    Task<RequestPasswordRecoveryResult> RequestPasswordRecoveryAsync(
        RequestPasswordRecoveryRequest request
    );

    Task<ValidatePasswordRecoveryTokenResult> ValidatePasswordRecoveryTokenAsync(
        ValidatePasswordRecoveryTokenRequest request
    );

    Task<ResetPasswordWithRecoveryResult> ResetPasswordWithRecoveryAsync(
        ResetPasswordWithRecoveryRequest request
    );
}
```

### Why a dedicated service

- This is not normal registration.
- This is not authenticated sign-in/sign-out behavior.
- It is an unauthenticated, token-based account-recovery flow with its own lifecycle.
- A dedicated service keeps the public surface easier to discover without overloading unrelated services.

## Proposed Models

```csharp
public record RequestPasswordRecoveryRequest(string Email);

public record ValidatePasswordRecoveryTokenRequest(string Token);

public record ResetPasswordWithRecoveryRequest(string Token, string Password);
```

### Request result

```csharp
public enum RequestPasswordRecoveryResultCode
{
    Success,
    UserNotFoundError,
    ValidationError
}

public record RequestPasswordRecoveryResult(
    RequestPasswordRecoveryResultCode ResultCode,
    string Message,
    string? RecoveryToken
);
```

- `Success` means the library created a recovery record and returned a token.
- `UserNotFoundError` means no user matched the supplied email.
- `RecoveryToken` is only populated when the library actually created a recovery record.
- The host decides whether to surface or suppress that distinction in end-user messaging.

### Validate result

```csharp
public enum ValidatePasswordRecoveryTokenResultCode
{
    Success,
    PasswordRecoveryNotFoundError,
    PasswordRecoveryExpiredError,
    PasswordRecoveryAlreadyUsedError,
    PasswordRecoveryInvalidatedError
}

public record ValidatePasswordRecoveryTokenResult(
    ValidatePasswordRecoveryTokenResultCode ResultCode,
    string Message
);
```

### Consume/reset result

```csharp
public enum ResetPasswordWithRecoveryResultCode
{
    Success,
    PasswordRecoveryNotFoundError,
    PasswordRecoveryExpiredError,
    PasswordRecoveryAlreadyUsedError,
    PasswordRecoveryInvalidatedError,
    PasswordValidationError
}

public record ResetPasswordWithRecoveryResult(
    ResetPasswordWithRecoveryResultCode ResultCode,
    string Message
);
```

## Domain Design

Use a **dedicated password-recovery domain/entity**, not invitations:

- invitations are account-scoped onboarding tokens
- password recovery is user-scoped account recovery
- invitations require a different acceptance model and authenticated email match
- recovery needs different invalidation and password-reset semantics

Proposed entity:

```csharp
internal class PasswordRecoveryEntity : IHasCreatedUtc
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public IHashedValue SecretHash { get; set; } = null!;
    public DateTime ExpiresUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public DateTime? InvalidatedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}
```

### Why hashed secret storage

- recovery tokens should not be stored in plaintext
- a dedicated entity lets the library look up the record by `Id` and verify the secret against a stored hash
- this avoids scanning all pending records while still avoiding raw token storage

## Token Format

Use an opaque two-part token:

```text
{recoveryId}.{secret}
```

- `recoveryId` = GUID/record identifier
- `secret` = random high-entropy secret, returned once
- database stores:
  - `Id`
  - hashed `secret`
  - lifecycle timestamps

Validation flow:

1. Parse token into `recoveryId` + `secret`
2. Load the recovery record by `Id`
3. Check pending/expired/used/invalidated state
4. Verify `secret` against the stored hash

## Lifecycle Rules

### Request

1. Validate email format
2. Find user by email
3. If no matching user exists, return `UserNotFoundError` with `RecoveryToken = null`
4. Invalidate any older pending recovery records for that user
5. Create a new record with `ExpiresUtc = now + 10 minutes`
6. Return the token once

### Validate

1. Parse token
2. Resolve the recovery record
3. Return one of:
   - `Success`
   - not found
   - expired
   - already used
   - invalidated

### Reset

1. Parse and validate the token
2. Validate the new password against existing password rules
3. Update or create the user's basic-auth credentials
4. Mark the recovery record `CompletedUtc`
5. Invalidate any other pending recovery records for that user
6. Revoke all current auth tokens for that user
7. Clear login lockout state (`LockedUtc`, failed-attempt counters) so the user can sign in cleanly

## Eligibility Rule

Password recovery should target **users identified by email**.

The safest initial behavior is:

- if the email does not match a user: explicit `UserNotFoundError`, no token
- if the user exists: allow recovery to establish a valid password-auth credential path

That means this flow can both:

- reset an existing password
- create a password for a user who currently authenticates only through Google

This is acceptable because the recovery channel is the user's email address. If the host is willing to trust email-based recovery, then letting recovery establish a new password is consistent with that trust boundary.

## Host / Library Boundary

The library should:

- generate and validate recovery tokens
- manage expiry/invalidation/consumption
- apply password validation and credential updates
- revoke sessions on successful reset

The host should:

- decide the user-facing response text
- decide whether to send email, SMS, or another notification
- deliver the returned token through its own communication channel
- decide how the token is embedded in a reset link/UI flow

## Service Registration / Authorization

- Register `IPasswordRecoveryService` as a normal scoped service
- Apply telemetry decorator
- Do **not** apply the existing user-context authorization decorator pattern, because recovery must be callable without authentication

## Implementation Shape

1. Add `PasswordRecoveries/`
   - `Entities/PasswordRecoveryEntity.cs`
   - `Processors/IPasswordRecoveryProcessor.cs`
   - `Processors/PasswordRecoveryProcessor.cs`
   - model/result files
2. Add `Services/IPasswordRecoveryService.cs`
3. Add `Services/PasswordRecoveryService.cs`
4. Register service and processor in `ServiceRegistrationExtensions.cs`
5. Add EF configuration + migrations for all providers
6. Add docs under:
   - `Docs/services/`
   - `Docs/domains/`
   - auth docs where relevant

## Test Plan

### Request tests

- known email creates token
- unknown email returns `UserNotFoundError` with no token
- invalid email returns validation error
- new request invalidates prior pending token
- expiry is set to 10 minutes

### Validate tests

- valid token succeeds
- malformed token returns not found/invalid
- unknown `Id` fails
- wrong secret fails
- expired token fails
- completed token fails
- invalidated token fails

### Reset tests

- valid token resets existing password
- valid token creates password credentials for a Google-only user
- invalid password returns password validation error
- successful reset marks token completed
- successful reset invalidates sibling pending tokens
- successful reset revokes all auth tokens
- successful reset clears lockout state

## Notes

- Keep this host-agnostic; email sending should not become a dependency of `Corely.IAM`.
- Reuse existing password validation, hashing, result-code, and token-lifecycle patterns where practical.
- The user-facing "If an account exists, we sent a recovery link" behavior belongs in the host, not in the library.
- Keep the TTL fixed at 10 minutes for the first implementation; if this ever needs to vary, add configuration later rather than making it request-driven.

## Status

- Plan created.
- Design decisions captured.
- Concrete API and lifecycle design drafted.
- No implementation started.
