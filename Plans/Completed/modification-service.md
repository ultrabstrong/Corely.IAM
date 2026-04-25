# Plan: ModificationService — Entity Property Updates

## Context

The service layer now covers Create (Registration), Read (Retrieval), and Delete (Deregistration). This adds the **U** — a `ModificationService` for updating scalar properties on entities. Relationship changes (add user to group, assign role, etc.) remain in Registration/Deregistration.

**Scope — 4 entities, updatable fields only:**

| Entity | Updatable Fields |
|--------|-----------------|
| **Account** | `AccountName` |
| **User** | `Username`, `Email` (self-only auth) |
| **Group** | `Name`, `Description` |
| **Role** | `Name`, `Description` (reject if `IsSystemDefined`) |

**Out of scope:** Permissions (delete + recreate), passwords (auth concern), login stats (system-managed), bulk updates, relationship changes.

---

## Phase 1: Shared Result Models

**New files in `Corely.IAM/Models/`:**

- **`ModifyResultCode.cs`**
    ```csharp
    public enum ModifyResultCode
    {
        Success,
        NotFoundError,
        UnauthorizedError,
        SystemDefinedError,
    }
    ```

- **`ModifyResult.cs`**
    ```csharp
    public record ModifyResult(ModifyResultCode ResultCode, string Message);
    ```

---

## Phase 2: Request Models + Validators

One request model per entity, placed in each domain's `Models/` folder. One validator per request model in each domain's `Validators/` folder.

### Request Models

| File | Record |
|------|--------|
| `Accounts/Models/UpdateAccountRequest.cs` | `(Guid AccountId, string AccountName)` |
| `Users/Models/UpdateUserRequest.cs` | `(Guid UserId, string Username, string Email)` |
| `Groups/Models/UpdateGroupRequest.cs` | `(Guid GroupId, string Name, string? Description)` |
| `Roles/Models/UpdateRoleRequest.cs` | `(Guid RoleId, string Name, string? Description)` |

### Validators

Mirror the existing domain model validators (reuse the same constants):

| File | Rules |
|------|-------|
| `Accounts/Validators/UpdateAccountRequestValidator.cs` | AccountName: NotEmpty, min/max length from `AccountConstants` |
| `Users/Validators/UpdateUserRequestValidator.cs` | Username: NotEmpty, min/max from `UserConstants`; Email: NotEmpty, EmailAddress, max from `UserConstants` |
| `Groups/Validators/UpdateGroupRequestValidator.cs` | Name: NotEmpty, min/max from `GroupConstants` |
| `Roles/Validators/UpdateRoleRequestValidator.cs` | Name: NotEmpty, min/max from `RoleConstants` |

Validators are auto-discovered by the existing FluentValidation registration in `ServiceRegistrationExtensions.cs` — no wiring needed.

---

## Phase 3: Processor Update Methods

Each processor gets an `UpdateAsync` method. Pattern: **fetch → guard → apply fields → save**.

### Entity Fix

**`Accounts/Entities/AccountEntity.cs`** — change `AccountName` from `init` to `set` to support in-place modification.

### Interface Additions

| Interface | New Method |
|-----------|-----------|
| `IAccountProcessor` | `Task<ModifyResult> UpdateAccountAsync(UpdateAccountRequest request)` |
| `IGroupProcessor` | `Task<ModifyResult> UpdateGroupAsync(UpdateGroupRequest request)` |
| `IRoleProcessor` | `Task<ModifyResult> UpdateRoleAsync(UpdateRoleRequest request)` |

**`IUserProcessor`** — refactor existing `UpdateUserAsync(User)` → `UpdateUserAsync(UpdateUserRequest)` returning `ModifyResult`.

### Implementation Pattern (example: Group)

```csharp
public async Task<ModifyResult> UpdateGroupAsync(UpdateGroupRequest request)
{
    _validationProvider.ThrowIfInvalid(request);
    var accountId = _userContextProvider.GetUserContext()?.CurrentAccount?.Id;
    var entity = await _groupRepo.GetAsync(e => e.Id == request.GroupId && e.AccountId == accountId);
    if (entity == null)
        return new(ModifyResultCode.NotFoundError, $"Group {request.GroupId} not found");
    entity.Name = request.Name;
    entity.Description = request.Description;
    await _groupRepo.UpdateAsync(entity);
    return new(ModifyResultCode.Success, string.Empty);
}
```

**Role-specific guard:**
```csharp
if (entity.IsSystemDefined)
    return new(ModifyResultCode.SystemDefinedError, "Cannot modify system-defined role");
```

**User-specific:** No account scoping (users are M:M with accounts). Fetch by `request.UserId` only. The auth decorator enforces self-only.

### Authorization Decorators

| Decorator | Auth Check |
|-----------|-----------|
| `AccountProcessorAuthorizationDecorator` | `IsAuthorizedAsync(AuthAction.Update, ACCOUNT_RESOURCE_TYPE, accountId)` |
| `UserProcessorAuthorizationDecorator` | `IsAuthorizedForOwnUser(userId)` (existing pattern) |
| `GroupProcessorAuthorizationDecorator` | `IsAuthorizedAsync(AuthAction.Update, GROUP_RESOURCE_TYPE, groupId)` |
| `RoleProcessorAuthorizationDecorator` | `IsAuthorizedAsync(AuthAction.Update, ROLE_RESOURCE_TYPE, roleId)` |

### Telemetry Decorators

Each domain's telemetry decorator wraps the new method with `ExecuteWithLoggingAsync`, following the existing pattern.

### Cleanup

- Delete `Users/Models/UpdateUserResult.cs` (`UpdateUserResult` + `UpdateUserResultCode` become unused)
- Update any references to the old types

---

## Phase 4: ModificationService

### Interface

**`Services/IModificationService.cs`**
```csharp
public interface IModificationService
{
    Task<ModifyResult> ModifyAccountAsync(UpdateAccountRequest request);
    Task<ModifyResult> ModifyUserAsync(UpdateUserRequest request);
    Task<ModifyResult> ModifyGroupAsync(UpdateGroupRequest request);
    Task<ModifyResult> ModifyRoleAsync(UpdateRoleRequest request);
}
```

### Implementation

**`Services/ModificationService.cs`** — thin delegation to processors:
```csharp
internal class ModificationService(
    IAccountProcessor accountProcessor,
    IUserProcessor userProcessor,
    IGroupProcessor groupProcessor,
    IRoleProcessor roleProcessor,
    ILogger<ModificationService> logger
) : IModificationService
{
    public async Task<ModifyResult> ModifyAccountAsync(UpdateAccountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        logger.LogInformation("Modifying account {AccountId}", request.AccountId);
        return await accountProcessor.UpdateAccountAsync(request);
    }
    // ... same thin pattern for each
}
```

### Authorization Decorator

**`Services/ModificationServiceAuthorizationDecorator.cs`** — coarse checks (matching Registration pattern):

| Method | Check |
|--------|-------|
| `ModifyAccountAsync` | `HasAccountContext()` |
| `ModifyUserAsync` | `HasUserContext()` |
| `ModifyGroupAsync` | `HasAccountContext()` |
| `ModifyRoleAsync` | `HasAccountContext()` |

Fine-grained permission checks happen at the processor decorator level.

### Telemetry Decorator

**`Services/ModificationServiceTelemetryDecorator.cs`** — `ExecuteWithLoggingAsync` for each method.

### DI Registration

**`ServiceRegistrationExtensions.cs`** — add after RetrievalService:
```csharp
serviceCollection.AddScoped<IModificationService, ModificationService>();
serviceCollection.Decorate<IModificationService, ModificationServiceAuthorizationDecorator>();
serviceCollection.Decorate<IModificationService, ModificationServiceTelemetryDecorator>();
```

---

## Phase 5: Tests

Processor-level tests for each domain (matching existing `*ListGetTests.cs` pattern):

| Test File | Test Cases |
|-----------|-----------|
| `AccountProcessorUpdateTests.cs` | ✅ Success, ❌ not found, ❌ wrong account, ❌ validation (empty name) |
| `UserProcessorUpdateTests.cs` | ✅ Success, ❌ not found, ❌ validation (empty username, invalid email) |
| `GroupProcessorUpdateTests.cs` | ✅ Success, ❌ not found, ❌ wrong account, ❌ validation |
| `RoleProcessorUpdateTests.cs` | ✅ Success, ❌ not found, ❌ wrong account, ❌ system-defined guard, ❌ validation |

Use `ServiceFactory` (mock DB) pattern, same as existing processor tests.

---

## Files Summary

### New Files (14)
| File | Purpose |
|------|---------|
| `Models/ModifyResultCode.cs` | Shared result code enum |
| `Models/ModifyResult.cs` | Shared result record |
| `Accounts/Models/UpdateAccountRequest.cs` | Request model |
| `Users/Models/UpdateUserRequest.cs` | Request model |
| `Groups/Models/UpdateGroupRequest.cs` | Request model |
| `Roles/Models/UpdateRoleRequest.cs` | Request model |
| `Accounts/Validators/UpdateAccountRequestValidator.cs` | Validator |
| `Users/Validators/UpdateUserRequestValidator.cs` | Validator |
| `Groups/Validators/UpdateGroupRequestValidator.cs` | Validator |
| `Roles/Validators/UpdateRoleRequestValidator.cs` | Validator |
| `Services/IModificationService.cs` | Service interface |
| `Services/ModificationService.cs` | Service implementation |
| `Services/ModificationServiceAuthorizationDecorator.cs` | Auth decorator |
| `Services/ModificationServiceTelemetryDecorator.cs` | Telemetry decorator |

### New Test Files (4)
| File | Purpose |
|------|---------|
| `UnitTests/Accounts/Processors/AccountProcessorUpdateTests.cs` | Account update tests |
| `UnitTests/Users/Processors/UserProcessorUpdateTests.cs` | User update tests |
| `UnitTests/Groups/Processors/GroupProcessorUpdateTests.cs` | Group update tests |
| `UnitTests/Roles/Processors/RoleProcessorUpdateTests.cs` | Role update tests |

### Modified Files (~12)
| File | Change |
|------|--------|
| `Accounts/Entities/AccountEntity.cs` | `AccountName`: `init` → `set` |
| `Accounts/Processors/IAccountProcessor.cs` | Add `UpdateAccountAsync` |
| `Accounts/Processors/AccountProcessor.cs` | Implement `UpdateAccountAsync` |
| `Accounts/Processors/AccountProcessorAuthorizationDecorator.cs` | Add auth wrapper |
| `Accounts/Processors/AccountProcessorTelemetryDecorator.cs` | Add telemetry wrapper |
| `Users/Processors/IUserProcessor.cs` | Refactor `UpdateUserAsync` signature |
| `Users/Processors/UserProcessor.cs` | Refactor `UpdateUserAsync` implementation |
| `Users/Processors/UserProcessorAuthorizationDecorator.cs` | Update signature |
| `Users/Processors/UserProcessorTelemetryDecorator.cs` | Update signature |
| `Groups/Processors/IGroupProcessor.cs` | Add `UpdateGroupAsync` |
| `Groups/Processors/GroupProcessor.cs` | Implement `UpdateGroupAsync` |
| `Groups/Processors/GroupProcessorAuthorizationDecorator.cs` | Add auth wrapper |
| `Groups/Processors/GroupProcessorTelemetryDecorator.cs` | Add telemetry wrapper |
| `Roles/Processors/IRoleProcessor.cs` | Add `UpdateRoleAsync` |
| `Roles/Processors/RoleProcessor.cs` | Implement `UpdateRoleAsync` |
| `Roles/Processors/RoleProcessorAuthorizationDecorator.cs` | Add auth wrapper |
| `Roles/Processors/RoleProcessorTelemetryDecorator.cs` | Add telemetry wrapper |
| `ServiceRegistrationExtensions.cs` | Register ModificationService + decorators |

### Deleted Files (1)
| File | Reason |
|------|--------|
| `Users/Models/UpdateUserResult.cs` | Replaced by shared `ModifyResult` |

---

## Verification

1. `dotnet build Corely.IAM.sln` — 0 errors, 0 warnings
2. `dotnet test Corely.IAM.UnitTests` — all tests pass
3. `.\RebuildAndTest.ps1` — format + rebuild + test (final check before commit)
