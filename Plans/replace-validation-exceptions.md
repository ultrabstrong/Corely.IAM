# Replace Validation Exceptions with Result Codes

## Goal

Replace the `ThrowIfInvalid` / `ValidationException` pattern with `ValidateAndLog` / result code returns across all processors. After this work, validation failures will be communicated via typed result codes (like every other error), and `ValidationException` + `ThrowIfInvalid` can be removed entirely.

## Reference Implementation

`InvitationProcessor.CreateInvitationAsync` already uses the target pattern:

```csharp
var validation = _validationProvider.ValidateAndLog(request);
if (!validation.IsValid)
{
    return new CreateInvitationResult(
        CreateInvitationResultCode.ValidationError,
        validation.Message,
        null, null
    );
}
```

## Current State

### ThrowIfInvalid call sites (11 total across 6 processors)

| Processor | Method | Model Validated |
|-----------|--------|-----------------|
| `UserProcessor` | `CreateUserAsync` | `User` |
| `UserProcessor` | `UpdateUserAsync` | `UpdateUserRequest` |
| `AccountProcessor` | `CreateAccountAsync` | `Account` |
| `AccountProcessor` | `UpdateAccountAsync` | `UpdateAccountRequest` |
| `GroupProcessor` | `CreateGroupAsync` | `Group` |
| `GroupProcessor` | `UpdateGroupAsync` | `UpdateGroupRequest` |
| `RoleProcessor` | `CreateRoleAsync` | `Role` |
| `RoleProcessor` | `UpdateRoleAsync` | `UpdateRoleRequest` |
| `PermissionProcessor` | `CreatePermissionAsync` | `Permission` |
| `BasicAuthProcessor` | `CreateBasicAuthAsync` | `BasicAuth` |
| `BasicAuthProcessor` | `UpdateBasicAuthAsync` | `BasicAuth` |

### Result code enums needing `ValidationError` member

**Create operations:**

| Enum | File |
|------|------|
| `CreateUserResultCode` | `Users/Models/CreateUserResult.cs` |
| `CreateAccountResultCode` | `Accounts/Models/CreateAccountResult.cs` |
| `CreateGroupResultCode` | `Groups/Models/CreateGroupResult.cs` |
| `CreateRoleResultCode` | `Roles/Models/CreateRoleResult.cs` |
| `CreatePermissionResultCode` | `Permissions/Models/CreatePermissionResult.cs` |
| `CreateBasicAuthResultCode` | `BasicAuths/Models/CreateBasicAuthResult.cs` |

**Update operations:**

| Enum | File |
|------|------|
| `ModifyResultCode` | `Models/ModifyResultCode.cs` (shared by User, Account, Group, Role updates) |
| `UpdateBasicAuthResultCode` | `BasicAuths/Models/UpdateBasicAuthResult.cs` |

**Already has `ValidationError`:** `CreateInvitationResultCode` (no change needed)

### DevTools catch blocks (31 files)

All DevTools command files catch `ValidationException` and display errors:

```csharp
catch (ValidationException ex)
{
    Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
}
```

After migration, validation errors will arrive as result codes. These try/catch blocks become dead code.

### Tests expecting ValidationException (7 test files)

| Test File | Assertion |
|-----------|-----------|
| `AccountProcessorTests.cs:217` | `Assert.IsType<ValidationException>(ex)` |
| `AccountProcessorUpdateTests.cs:105` | `Assert.ThrowsAsync<ValidationException>` |
| `GroupProcessorTests.cs:180` | `Assert.IsType<ValidationException>(ex)` |
| `GroupProcessorUpdateTests.cs:110` | `Assert.ThrowsAsync<ValidationException>` |
| `RoleProcessorTests.cs:143` | `Assert.IsType<ValidationException>(await ex)` |
| `RoleProcessorUpdateTests.cs:127` | `Assert.ThrowsAsync<ValidationException>` |
| `UserProcessorUpdateTests.cs:96,107` | `Assert.ThrowsAsync<ValidationException>` (2 sites) |

### Validator infrastructure to clean up

| File | What to remove/change |
|------|----------------------|
| `Validators/ValidationException.cs` | Delete entirely |
| `Validators/ValidationResult.cs` | Remove `ThrowIfInvalid()` method |
| `Validators/IValidationProvider.cs` | Remove `ThrowIfInvalid<T>()` method |
| `Validators/FluentValidators/FluentValidationProvider.cs` | Remove `ThrowIfInvalid<T>()` method |
| `Validators/FluentValidators/FluentValidationProviderTests.cs` | Remove/update ThrowIfInvalid test |
| `Validators/ValidationResultTests.cs` | Remove ThrowIfInvalid test |
| `Validators/ValidationExceptionTests.cs` | Delete entirely |

---

## Implementation Phases

### Phase 1: Add `ValidationError` to result code enums

Add `ValidationError` member to:
- `CreateUserResultCode`
- `CreateAccountResultCode`
- `CreateGroupResultCode`
- `CreateRoleResultCode`
- `CreatePermissionResultCode`
- `CreateBasicAuthResultCode`
- `ModifyResultCode`
- `UpdateBasicAuthResultCode`

### Phase 2: Migrate processors (per-domain)

For each of the 11 call sites, replace:

```csharp
_validationProvider.ThrowIfInvalid(model);
```

With:

```csharp
var validation = _validationProvider.ValidateAndLog(model);
if (!validation.IsValid)
{
    return new XxxResult(XxxResultCode.ValidationError, validation.Message, ...);
}
```

**Domain order** (independent — can be parallelized):

1. **Users** — `UserProcessor.CreateUserAsync`, `UserProcessor.UpdateUserAsync`
2. **Accounts** — `AccountProcessor.CreateAccountAsync`, `AccountProcessor.UpdateAccountAsync`
3. **Groups** — `GroupProcessor.CreateGroupAsync`, `GroupProcessor.UpdateGroupAsync`
4. **Roles** — `RoleProcessor.CreateRoleAsync`, `RoleProcessor.UpdateRoleAsync`
5. **Permissions** — `PermissionProcessor.CreatePermissionAsync`
6. **BasicAuths** — `BasicAuthProcessor.CreateBasicAuthAsync`, `BasicAuthProcessor.UpdateBasicAuthAsync`

### Phase 3: Update tests

For each test that asserts `ValidationException`:
- Change to assert the result code is `ValidationError` and the message contains validation info
- Remove `using Corely.IAM.Validators` where `ValidationException` is no longer referenced

### Phase 4: Remove DevTools try/catch blocks

Remove the `try/catch (ValidationException)` blocks from all 31 DevTools command files. The result is already serialized and printed — validation errors will now appear in the result JSON output like every other error.

### Phase 5: Delete validation exception infrastructure

1. Delete `Validators/ValidationException.cs`
2. Delete `Validators/ValidationExceptionTests.cs`
3. Remove `ThrowIfInvalid()` from `ValidationResult.cs`
4. Remove `ThrowIfInvalid<T>()` from `IValidationProvider.cs`
5. Remove `ThrowIfInvalid<T>()` from `FluentValidationProvider.cs`
6. Update `FluentValidationProviderTests.cs` — remove ThrowIfInvalid test
7. Update `ValidationResultTests.cs` — remove ThrowIfInvalid test

---

## File Count Estimate

| Phase | Files |
|-------|-------|
| Phase 1 — Enum updates | 8 |
| Phase 2 — Processor migrations | 6 |
| Phase 3 — Test updates | 7 |
| Phase 4 — DevTools catch removal | 31 |
| Phase 5 — Infrastructure cleanup | 7 |
| **Total** | **~59** |
