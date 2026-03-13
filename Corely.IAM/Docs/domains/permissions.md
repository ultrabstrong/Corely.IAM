# Permissions

CRUDX permission model scoped to a resource type and optional resource ID. Permissions are assigned to roles, not directly to users.

## Model Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `Description` | `string?` | Optional description |
| `AccountId` | `Guid` | Owning account |
| `ResourceType` | `string` | Resource type (see [Resource Types](../resource-types.md)) |
| `ResourceId` | `Guid` | Specific resource ID, or `Guid.Empty` for wildcard |
| `Create` | `bool` | Create action granted |
| `Read` | `bool` | Read action granted |
| `Update` | `bool` | Update action granted |
| `Delete` | `bool` | Delete action granted |
| `Execute` | `bool` | Execute action granted |
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

## Wildcard Support

- **Resource type `"*"`** — grants the action on all resource types
- **Resource ID `Guid.Empty`** — grants the action on all resources of the specified type
- Combining both (`"*"` + `Guid.Empty`) grants full access for the specified actions

## Effective Permission Tree

Effective permissions show how a user's access is derived:

```
EffectivePermission (CRUDX flags, ResourceType, ResourceId)
├── EffectiveRole (RoleId, RoleName, IsDirect)
│   └── EffectiveGroup[] (GroupId, GroupName)
└── ...
```

- `IsDirect = true` — role assigned directly to the user
- `Groups` — groups through which the role is inherited

Retrieve effective permissions by passing `hydrate: true` to `IRetrievalService` get methods.

## Key Behaviors

- Permissions are immutable after creation — delete and recreate to change
- At least one CRUDX flag must be `true` (validated by `PermissionValidator`)
- Resource type must exist in `IResourceTypeRegistry` (validated by `PermissionValidator`)
- Permissions are account-scoped — they cannot cross account boundaries

## Result Codes

| Code | Meaning |
|------|---------|
| `CreatePermissionResultCode.Success` | Permission created |
| `CreatePermissionResultCode.PermissionExistsError` | Duplicate resource type + ID in account |
| `CreatePermissionResultCode.ValidationError` | Invalid resource type or no CRUDX flags |
| `DeletePermissionResultCode.SystemDefinedPermissionError` | Cannot delete system permission |
