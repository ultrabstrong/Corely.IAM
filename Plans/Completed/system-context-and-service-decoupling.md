# System Context & Service Authorization Decoupling

## Problem

The service layer tightly couples authorization with business operations by pulling `userId` and `accountId` directly from the authenticated user context. This creates two issues:

1. **Headless/background processes can't use the service APIs** — they have no authenticated user, but services are the only public API (processors are `internal`).
2. **`HasAccountContext()` is imprecise** — it verifies *some* account context exists, without validating that it matches the target account in the request.

There are two categories of context usage in services:

| Category | Usage | Examples |
|----------|-------|---------|
| **Identity ("this is me")** | `context.User.Id` for self-operations | SetPassword, MFA, GoogleAuth, DeregisterUser |
| **Targeting ("operate on this scope")** | `context.CurrentAccount.Id` or `context.User.Id` as owner | RegisterGroup, RegisterRole, RegisterAccount |

Category 1 is correct — these are genuinely "self" operations. Category 2 uses context as a hidden parameter store and should be made explicit.

## Approach

All changes ship together as one coordinated set:

1. **System Context** — a new mode on `UserContext` that bypasses authorization for trusted internal callers
2. **Request Model Refactoring** — move targeting IDs into request models
3. **HasAccountContext(Guid)** — validate caller access to the specific target account
4. **Category 1 Explicit Rejection** — system context explicitly rejected for self-operations
5. **Cleanup & Tests**

---

## 1. System Context

### Design

System context is **not a separate object** — it's a new mode on the existing `UserContext`. This leverages the existing scoped DI lifetime for automatic cleanup (no `IDisposable` needed).

### UserContext Changes

Add `IsSystemContext` property and a new constructor:

```csharp
public record UserContext
{
    // Existing constructor for normal user context
    public UserContext(User user, Account? currentAccount, string deviceId, List<Account> availableAccounts)
    {
        User = user;
        CurrentAccount = currentAccount;
        DeviceId = deviceId;
        AvailableAccounts = availableAccounts;
        IsSystemContext = false;
    }

    // New constructor for system context
    public UserContext(bool isSystemContext, string? deviceId = null)
    {
        IsSystemContext = isSystemContext;
        DeviceId = deviceId ?? "system";
        AvailableAccounts = [];
    }

    public User? User { get; init; }
    public Account? CurrentAccount { get; init; }
    public string DeviceId { get; init; }
    public List<Account> AvailableAccounts { get; init; }
    public bool IsSystemContext { get; init; }
}
```

`User` and `CurrentAccount` are intentionally null in system context — no fake data.

### IUserContextSetter Changes

Add a method to set system context. This is how consumers (background jobs, Azure Functions, etc.) establish system context:

```csharp
public interface IUserContextSetter
{
    void SetUserContext(UserContext context);    // existing
    void ClearUserContext(Guid userId);          // existing
    void SetSystemContext(string? deviceId = null); // new
}
```

`SetSystemContext` internally creates `new UserContext(isSystemContext: true, deviceId)` and stores it via the same mechanism as `SetUserContext`. Consumers call this instead of going through the sign-in flow.

### AuthorizationProvider Changes

No new interfaces or injections needed. The existing `IUserContextProvider` already provides the context — just check `IsSystemContext` on it.

Authorization behavior by method:

| Method | System Context Behavior | Why |
|--------|------------------------|-----|
| `HasUserContext()` | `true` | System is authenticated (context exists) |
| `HasAccountContext(Guid)` | `true` | System can operate on any account |
| `IsAuthorizedAsync(...)` | `true` | System bypasses CRUDX checks |
| `IsAuthorizedForOwnUser(...)` | **`false`** | System is NOT a user — cannot be "self" |

`IsAuthorizedForOwnUser` returning `false` is a security feature: it prevents system context from being used for self-operations (password changes, MFA, user deletion, etc.) that should only ever be performed by the actual user.

---

## 2. Request Model Refactoring

Move "targeting" IDs from context into request models. Services use request parameters instead of context lookups.

### RegistrationService

| Method | Change |
|--------|--------|
| `RegisterAccountAsync` | Add `OwnerUserId` to `RegisterAccountRequest` |
| `RegisterGroupAsync` | Add `AccountId` to `RegisterGroupRequest` |
| `RegisterRoleAsync` | Add `AccountId` to `RegisterRoleRequest` |
| `RegisterPermissionAsync` | Add `AccountId` to `RegisterPermissionRequest` |
| `RegisterUserWithAccountAsync` | Add `AccountId` to `RegisterUserWithAccountRequest` |
| `RegisterUsersWithGroupAsync` | Add `AccountId` to `RegisterUsersWithGroupRequest` |
| `RegisterRolesWithGroupAsync` | Add `AccountId` to `RegisterRolesWithGroupRequest` |
| `RegisterRolesWithUserAsync` | Add `AccountId` to `RegisterRolesWithUserRequest` |
| `RegisterPermissionsWithRoleAsync` | Add `AccountId` to `RegisterPermissionsWithRoleRequest` |

### DeregistrationService

Audit all methods that pull `context.CurrentAccount.Id` — add `AccountId` to their request models.

### RetrievalService

Audit all methods that pull from context for account-scoped queries — add `AccountId` to requests.

### ModificationService

Audit all methods that pull from context for account-scoped operations — add `AccountId` to requests.

### Context Mutation Guards

Some services mutate context after operations (e.g., adding to `AvailableAccounts` after account creation). These need guards since system context has no real user context to mutate:

```csharp
var context = _userContextProvider.GetUserContext();
if (!context!.IsSystemContext)
{
    context.AvailableAccounts.Add(new Account { ... });
}
```

Similarly, `ClearUserContext` and `ClearCache` calls need guards for system context scenarios.

---

## 3. HasAccountContext(Guid accountId)

### Interface Change

```csharp
// Before
bool HasAccountContext();

// After
bool HasAccountContext(Guid accountId);
```

### Implementation

For **user context**: verify ALL of the following (preserving existing security guarantees):
1. User context exists
2. `CurrentAccount` is not null
3. `CurrentAccount.Id == accountId` (new — validates target matches signed-in account)
4. `CurrentAccount.Id` is in `AvailableAccounts` (existing check — preserved)

For **system context**: return `true` (bypass).

### Service Decorator Updates

All service authorization decorators that currently call `HasAccountContext()` change to pass `request.AccountId`:

```csharp
// Before
public async Task<RegisterGroupResult> RegisterGroupAsync(RegisterGroupRequest request) =>
    _authorizationProvider.HasAccountContext()
        ? await _inner.RegisterGroupAsync(request)
        : ...

// After
public async Task<RegisterGroupResult> RegisterGroupAsync(RegisterGroupRequest request) =>
    _authorizationProvider.HasAccountContext(request.AccountId)
        ? await _inner.RegisterGroupAsync(request)
        : ...
```

### RegisterAccount Special Case

`RegisterAccountAsync` uses `HasUserContext()` (not `HasAccountContext`) since you're creating a new account, not operating within one. Add `IsAuthorizedForOwnUser(request.OwnerUserId)` to the decorator to prevent users from creating accounts owned by someone else:

```csharp
public async Task<RegisterAccountResult> RegisterAccountAsync(RegisterAccountRequest request) =>
    _authorizationProvider.HasUserContext()
    && _authorizationProvider.IsAuthorizedForOwnUser(request.OwnerUserId)
        ? await _inner.RegisterAccountAsync(request)
        : ...
```

For system context: `HasUserContext()` returns `true`, `IsAuthorizedForOwnUser()` returns `false` — so system context is blocked here by default. This is intentional: creating accounts for arbitrary users is a sensitive operation. If system-context account creation is needed in the future, a dedicated service method should be added.

---

## 4. Category 1 Explicit Rejection

Category 1 "self" operations (`SetPassword`, `DeregisterUser`, MFA, GoogleAuth) must explicitly reject system context with a clear result code — not rely on null reference exceptions from accessing `context.User`.

### Pattern

Each Category 1 service method checks `IsSystemContext` early and returns an explicit error:

```csharp
public async Task<SetPasswordResult> SetPasswordAsync(SetPasswordRequest request)
{
    var context = _userContextProvider.GetUserContext();
    if (context!.IsSystemContext)
        return new SetPasswordResult(
            SetPasswordResultCode.SystemContextNotSupportedError,
            "Operation requires user context");

    var createResult = await _basicAuthProcessor.CreateBasicAuthAsync(
        new(context.User!.Id, request.Password));
    // ...
}
```

### New Result Code

Add `SystemContextNotSupportedError` to result code enums used by Category 1 operations. This provides consumers with an actionable error rather than an opaque failure.

### Defense in Depth

Category 1 operations are protected at two levels:
1. **Service level** — explicit `IsSystemContext` check with clear error result
2. **Processor level** — `IsAuthorizedForOwnUser()` returns `false` for system context, blocking at the decorator even if the service check is missed

---

## 5. Cleanup & Tests

### Cleanup

- Remove `IUserContextProvider` from services that only used it for Category 2 IDs
- Keep `IUserContextProvider` in services that use it for Category 1 "self" operations (MfaService, GoogleAuthService, AuthenticationService, etc.)
- Audit for any remaining `GetUserContext()!.CurrentAccount!.Id` patterns — these should all be replaced

### Tests

- Unit tests for `UserContext` system context constructor
- Unit tests for `SetSystemContext` on `UserContextProvider`
- Unit tests for `AuthorizationProvider` system context behavior (all 4 methods, especially `IsAuthorizedForOwnUser` returning `false`)
- Unit tests for Category 1 service methods returning `SystemContextNotSupportedError`
- Update existing service tests for new request model signatures
- Update existing decorator tests for `HasAccountContext(Guid)`
- Verify `RegisterAccountAsync` decorator rejects system context (via `IsAuthorizedForOwnUser`)

---

## Security Notes

### System context is NOT a super admin
System context bypasses authorization but does not grant user identity. It cannot:
- Perform self-operations (password, MFA, GoogleAuth, user deletion)
- Create accounts (blocked by `IsAuthorizedForOwnUser` returning false)
- Impersonate a user

It CAN perform account-scoped CRUD operations (groups, roles, permissions, etc.) when the accountId is passed explicitly.

### No security regressions
- `HasAccountContext(Guid)` **strengthens** the existing check by validating target account matches signed-in account AND preserving the `AvailableAccounts` membership check
- `IsAuthorizedForOwnUser` returning `false` for system context is **more restrictive** than the current design (which has no system context concept at all)
- Category 1 explicit rejection adds a **new** safety layer that doesn't exist today

### Scoping and lifecycle
- System context lives on `UserContext`, which is scoped — automatically cleaned up when the DI scope ends
- No `IDisposable` or manual cleanup needed — the DI container handles it
- Each background job / Azure Function invocation gets its own DI scope and therefore its own context

### Pre-existing concern (not addressed here)
`IsAuthorizedAsync()` loads permissions from `context.CurrentAccount` but does not independently verify that the target resource belongs to that account. This is a pre-existing gap unrelated to our changes. `HasAccountContext(Guid)` at the service decorator level mitigates this by ensuring the target account matches the signed-in account before any processor call. A future enhancement could add accountId validation directly into `IsAuthorizedAsync` as deeper defense-in-depth.

### Breaking changes
Request model constructors change (new required parameters). All consumers must update call sites. Acceptable since there is currently one consumer.
