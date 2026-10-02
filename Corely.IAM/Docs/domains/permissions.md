# Permissions

CRUDX permission model scoped to a resource type and optional resource ID. Permissions are assigned to roles, not directly to users.

## Model Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `Description` | `string?` | Optional description |
| `AccountId` | `Guid` | Owning account |
| `ResourceType` | `string` | Resource type (see [Resource Types](../resource-types.md)) |
| `ResourceId` | `Guid` | Specific resource ID, or `Guid.Empty` for every resource of the type |
| `Create` | `bool` | Create action granted |
| `Read` | `bool` | Read action granted |
| `Update` | `bool` | Update action granted |
| `Delete` | `bool` | Delete action granted |
| `Execute` | `bool` | Execute action granted |
| `IsSystemDefined` | `bool` | An Owner role default; cannot be deleted or detached from the Owner role |
| `Roles` | `List<ChildRef>?` | Roles that include this permission (hydrated) |

## CRUDX Model

Each permission grants one or more of five actions:

| Action | Typical Use |
|--------|-------------|
| Create | Register new entities |
| Read | List and view entities |
| Update | Modify entity properties |
| Delete | Remove entities |
| Execute | Perform non-CRUD operations |

## Resource Scope

- **Resource type**: always exactly one type. There is no wildcard type, and `"*"` is rejected
- **Resource ID `Guid.Empty`**: grants the action on all resources of the specified type

## Granting

Creating a permission succeeds only if the caller already holds every action it allows, on the same
type, covering the same resource ID. Otherwise the result is `UnauthorizedError` with "Cannot grant
permissions you do not hold". System context bypasses the rule. See
[Granting](../authorization.md#granting).

## Effective Permission Tree

Effective permissions show how a user's access is derived:

```
EffectivePermission (CRUDX flags, ResourceType, ResourceId)
├── EffectiveRole (RoleId, RoleName, IsDirect)
│   └── EffectiveGroup[] (GroupId, GroupName)
└── ...
```

- `IsDirect = true`: role assigned directly to the user
- `Groups`: groups through which the role is inherited

Retrieve effective permissions by passing `hydrate: true` to `IRetrievalService` get methods.

## Key Behaviors

- Permissions are immutable after creation. Delete and recreate to change
- At least one CRUDX flag must be `true` (validated by `PermissionValidator`)
- Resource type must exist in `IResourceTypeRegistry` (validated by `PermissionValidator`)
- Permissions are account-scoped and cannot cross account boundaries

## Result Codes

| Code | Meaning |
|------|---------|
| `CreatePermissionResultCode.Success` | Permission created |
| `CreatePermissionResultCode.PermissionExistsError` | The account already has a permission with the same type, resource ID and actions. The message names it and gives its ID |
| `CreatePermissionResultCode.ValidationError` | Invalid resource type or no CRUDX flags |
| `CreatePermissionResultCode.UnauthorizedError` | No permission to create permissions, or the caller does not hold what it grants |
| `DeletePermissionResultCode.SystemDefinedPermissionError` | Cannot delete system permission |
