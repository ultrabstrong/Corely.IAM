# Roles

Named collection of permissions, scoped to an account. Roles can be assigned directly to users or to groups.

## Model Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `Name` | `string` | Role name (1-100 characters) |
| `Description` | `string?` | Optional description |
| `IsSystemDefined` | `bool` | `true` for system roles (cannot be deleted) |
| `AccountId` | `Guid` | Owning account |
| `Users` | `List<ChildRef>?` | Users with this role (hydrated) |
| `Groups` | `List<ChildRef>?` | Groups with this role (hydrated) |
| `Permissions` | `List<ChildRef>?` | Permissions in this role (hydrated) |

## Constants

| Constant | Value |
|----------|-------|
| `RoleConstants.OWNER_ROLE_NAME` | `"Owner Role"` |
| `RoleConstants.ROLE_NAME_MIN_LENGTH` | `1` |
| `RoleConstants.ROLE_NAME_MAX_LENGTH` | `100` |

## System-Defined Roles

The **Owner Role** is created automatically when an account is registered. It has full CRUDX permissions on all resource types (wildcard). System-defined roles cannot be deleted or renamed.

## Relationships

- **Account** — belongs to one account
- **Users** — M:M (direct assignment)
- **Groups** — M:M (indirect assignment)
- **Permissions** — M:M

## Result Codes

| Code | Meaning |
|------|---------|
| `CreateRoleResultCode.Success` | Role created |
| `CreateRoleResultCode.RoleExistsError` | Duplicate name in account |
| `DeleteRoleResultCode.SystemDefinedRoleError` | Cannot delete system role |
