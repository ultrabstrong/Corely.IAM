# Consolidate LeaveAccountAsync into DeregisterUserFromAccountAsync

## Problem

`DeregisterUserFromAccountAsync` and `LeaveAccountAsync` do nearly the same thing — both call `RemoveUserFromAccountAsync` on the account processor. The only differences are:

1. **Auth gating**: `DeregisterUserFromAccountAsync` requires `HasAccountContext` (admin path), while `LeaveAccountAsync` requires `IsAuthorizedForOwnUser` (self path).
2. **Context mutation**: Both update `AvailableAccounts` and clear the auth cache on success, but the logic is slightly different between them.

Having two methods creates duplicated code across 9+ files (interface, service, auth decorator, telemetry decorator, tests, docs, Web pages). The admin path also contradictorily checks `request.UserId == context.User.Id` — self-detection logic that belongs only in the self path.

## Solution

Consolidate into a single `DeregisterUserFromAccountAsync` method with a dual auth check in the decorator:

```
HasAccountContext(request.AccountId) || IsAuthorizedForOwnUser(request.UserId)
```

- **Admin path**: passes via `HasAccountContext` (account-level permissions). System context also passes here.
- **Self path**: passes via `IsAuthorizedForOwnUser` (user removing themselves, no account permissions needed). System context correctly rejected.

The service implementation stays as one clean code path. The self-detection logic for context mutation (`request.UserId == context.User.Id`) remains — it's correct there because it governs whether to update the in-memory context, not authorization.

Callers using the self path just pass their own `UserId` in the request.

## Changes

### Core Library (Corely.IAM)

1. **`IDeregistrationService.cs`** — Remove `LeaveAccountAsync` from interface.
2. **`DeregistrationService.cs`** — Delete `LeaveAccountAsync` method. Keep `DeregisterUserFromAccountAsync` as-is (the context mutation logic on line 226-231 is correct for both paths).
3. **`DeregistrationServiceAuthorizationDecorator.cs`** — Delete `LeaveAccountAsync`. Update `DeregisterUserFromAccountAsync` to use dual auth: `HasAccountContext(request.AccountId) || IsAuthorizedForOwnUser(request.UserId)`.
4. **`DeregistrationServiceTelemetryDecorator.cs`** — Delete `LeaveAccountAsync`.

### Result Codes

5. **`DeregisterUserFromAccountResultCode`** — Verify `SystemContextNotSupportedError` can be removed since system context is now handled correctly via the dual auth check (system context passes `HasAccountContext` but fails `IsAuthorizedForOwnUser` — either way it gets through or gets rejected appropriately at the decorator level, not the service level).

### Tests

6. **`DeregistrationServiceAuthorizationDecoratorTests.cs`** — Remove `LeaveAccountAsync` tests. Add tests for the dual auth check on `DeregisterUserFromAccountAsync` (passes when either condition is true, fails when both are false).
7. **`DeregistrationServiceTelemetryDecoratorTests.cs`** — Remove `LeaveAccountAsync` tests.
8. **`DeregistrationServiceTests.cs`** — Remove `LeaveAccountAsync` tests if any exist.

### Consuming Projects

9. **`Corely.IAM.Web/Components/Pages/Profile.razor`** — Replace `LeaveAccountAsync(accountId)` call with `DeregisterUserFromAccountAsync(new DeregisterUserFromAccountRequest(userId, accountId))`.
10. **`Corely.IAM.ConsoleTest/Program.cs`** — Update if `LeaveAccountAsync` is called.
11. **`Corely.IAM.DevTools/`** — Update any commands that reference `LeaveAccountAsync`.

### Documentation

12. **`Corely.IAM/Docs/services/deregistration.md`** — Remove `LeaveAccountAsync` documentation.
13. **`Corely.IAM.Web/Docs/pages/profile.md`** — Update to reflect the consolidated method.
