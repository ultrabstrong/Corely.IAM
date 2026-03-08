# Remove Admin and Reader Default Roles/Permissions

## Problem Statement

When an account is registered, three system-defined roles and three system-defined permissions are automatically created: Owner, Admin, and Reader. Analysis shows that only the **Owner** role and its matching permission are actually enforced by the system. The Admin and Reader roles are never checked by name in any authorization, ownership, or protection logic — they are purely "convenience" starting points that add unnecessary complexity and data.

## Research Findings

### What Gets Created Today

`RegistrationService.RegisterAccountAsync` calls:
1. `RoleProcessor.CreateDefaultSystemRolesAsync` → creates **Owner**, **Admin**, **Reader** roles (all `IsSystemDefined = true`)
2. `PermissionProcessor.CreateDefaultSystemPermissionsAsync` → creates three matching permissions:
   - Owner: CRUDX on `*` (all resources, wildcard ID)
   - Admin: CRUdX on `*`
   - Reader: cRudx on `*`

### What Is Actually Enforced

Only `OWNER_ROLE_NAME` is ever checked by name in business logic:

| File | References | Purpose |
|------|-----------|---------|
| `UserOwnershipProcessor.cs` | 5× `OWNER_ROLE_NAME` | "Last owner" checks — prevent removing sole owner |
| `GroupProcessor.cs` | 3× `OWNER_ROLE_NAME` | Prevent removing owner role from group with no backup owner |
| `UserProcessor.cs` | 1× `OWNER_ROLE_NAME` | Prevent removing sole owner's role assignment |
| `RoleProcessor.cs` | 1× `OWNER_ROLE_NAME` | Protect owner system permission from removal |

`ADMIN_ROLE_NAME` and `READER_ROLE_NAME` appear **only** in:
- Their constant definitions (`RoleConstants.cs`)
- Creation code (`RoleProcessor.cs`, `PermissionProcessor.cs`)
- Tests

### Return Value Usage Gap

`CreateDefaultSystemRolesResult` has three IDs (`OwnerRoleId`, `AdminRoleId`, `ReaderRoleId`). In `RegistrationService.cs` only `rolesResult.OwnerRoleId` is used to assign the owner role to the registering user. `AdminRoleId` and `ReaderRoleId` are silently discarded.

### Deletion/Modification Protection

The `IsSystemDefined` flag protects roles and permissions from being deleted or renamed by users. This flag-based check does not depend on the role name, so removing Admin/Reader from creation does not affect the Owner protection logic.

### No Migration Needed

Existing accounts will retain their Admin and Reader roles/permissions harmlessly — they are never enforced by the authorization system. No schema changes are required; this is strictly a reduction in data created at registration time.

## Approach

Remove Admin and Reader from the default system setup. Simplify the creation methods and their result types to only deal with the Owner role/permission.

## Files to Change

### Production Code

1. **`Corely.IAM/Roles/Constants/RoleConstants.cs`**
   - Remove `ADMIN_ROLE_NAME` and `READER_ROLE_NAME` constants

2. **`Corely.IAM/Roles/Models/CreateDefaultSystemRolesResult.cs`**
   - Simplify record from `(OwnerRoleId, AdminRoleId, ReaderRoleId)` to `(OwnerRoleId)`

3. **`Corely.IAM/Roles/Processors/RoleProcessor.cs`**
   - `CreateDefaultSystemRolesAsync`: create only the Owner role; return simplified `CreateDefaultSystemRolesResult(ownerRole.Id)`

4. **`Corely.IAM/Permissions/Processors/PermissionProcessor.cs`**
   - `CreateDefaultSystemPermissionsAsync`: create only the Owner permission (CRUDX on `*`)
   - Remove the `adminRole` and `userRole` lookups and their permission entries

### Tests

5. **`Corely.IAM.UnitTests/Roles/Processors/RoleProcessorTests.cs`**
   - `CreateDefaultSystemRoles_CreatesDefaultRoles`: expect 1 role (Owner only), remove assertions for `AdminRoleId`/`ReaderRoleId`

6. **`Corely.IAM.UnitTests/Permissions/Processors/PermissionProcessorTests.cs`**
   - `CreateDefaultRolesAsync` helper: create only the Owner role
   - `CreateDefaultSystemPermissions_CreatesThreePermissions`: rename/update to assert 1 permission only
   - Remove `CreateDefaultSystemPermissions_CreatesAdminPermission_WithoutDelete`
   - Remove `CreateDefaultSystemPermissions_CreatesUserPermission_ReadOnly`
   - Keep/update `CreateDefaultSystemPermissions_CreatesOwnerPermission_WithFullAccess`

7. **`Corely.IAM.UnitTests/Services/RegistrationServiceTests.cs`**
   - Mock setup for `CreateDefaultSystemRolesAsync`: return `new CreateDefaultSystemRolesResult(ownerRoleId)` (single ID)

## Todos

- [ ] Remove `ADMIN_ROLE_NAME` / `READER_ROLE_NAME` from `RoleConstants.cs`
- [ ] Simplify `CreateDefaultSystemRolesResult` to single `OwnerRoleId`
- [ ] Update `RoleProcessor.CreateDefaultSystemRolesAsync` — owner role only
- [ ] Update `PermissionProcessor.CreateDefaultSystemPermissionsAsync` — owner permission only
- [ ] Update `RoleProcessorTests` — expect 1 default role
- [ ] Update `PermissionProcessorTests` — expect 1 default permission, remove admin/reader tests
- [ ] Update `RegistrationServiceTests` mock — single-ID result
- [ ] Run `.\RebuildAndTest.ps1` to verify
