# Codebase Consistency Follow-Up

## Problem

A full repository audit found a small set of real follow-up gaps that look missing or stale relative to the rest of the implemented codebase. These are not net-new features; they are consistency, wiring, test-coverage, and documentation fixes for behavior that already exists.

## Confirmed Scope

1. **TOTP time abstraction consistency**
   - `TotpAuthProcessor` still uses `DateTime.UtcNow` for recovery-code usage instead of the repo-standard `TimeProvider`.

2. **Missing public request validation wiring**
   - Google/TOTP request models used at the service layer do not currently have the same validator coverage and validation wiring as comparable request-driven flows.
   - Scope:
     - `LinkGoogleAuthRequest`
     - `RegisterUserWithGoogleRequest`
     - `ConfirmTotpRequest`
     - `DisableTotpRequest`

3. **Missing tests for already-wired code paths**
   - Missing processor decorator tests for:
     - `TotpAuthProcessorAuthorizationDecorator`
     - `TotpAuthProcessorTelemetryDecorator`
     - `GoogleAuthProcessorAuthorizationDecorator`
     - `GoogleAuthProcessorTelemetryDecorator`
   - Missing service implementation tests for:
     - `MfaService`
     - `GoogleAuthService`

4. **Stale docs / script guidance**
   - Several docs still describe every service as having an authorization decorator, which is no longer true after the no-op service decorator removals.
   - `RemoveMigration.ps1` still references the old `corely-db` tool name.
   - `authorization.md` should more explicitly describe `HasAccountContext(accountId)` and system-context behavior.

## Out of Scope

- New features or new public IAM capabilities
- Reopening the completed auth-boundary migration plan
- Historical completed plan documents that are intentionally preserved as snapshots
- Speculative cleanup that is not clearly inconsistent with existing code patterns

## Todos

- followup-timeprovider-totp
- followup-request-validation
- followup-missing-tests
- followup-stale-docs

## Notes

- Implement validators only where they are also wired into live request handling.
- Keep behavior-safe result mappings; avoid broad surface churn unless required.
- Treat documentation updates as part of the implementation, not as a separate optional pass.

## Status

- Completed.
- `TotpAuthProcessor` now uses `TimeProvider` for recovery-code consumption timestamps.
- Added and wired validators for the scoped Google/TOTP service-layer request models.
- Added the previously missing TOTP/Google processor decorator tests plus `MfaService` and `GoogleAuthService` implementation tests.
- Updated the stale contributor/docs/script guidance identified in the audit.
