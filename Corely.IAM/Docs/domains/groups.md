# Groups

Container for users and roles, scoped to an account. Groups provide indirect permission inheritance — users in a group receive all permissions from the group's assigned roles.

## Model Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `Name` | `string` | Group name |
| `Description` | `string?` | Optional description |
| `AccountId` | `Guid` | Owning account |
| `Users` | `List<ChildRef>?` | Users in this group (hydrated) |
| `Roles` | `List<ChildRef>?` | Roles assigned to this group (hydrated) |

## Relationships

- **Account** — belongs to one account
- **Users** — M:M
- **Roles** — M:M

## Key Behaviors

- Deleting a group that contains the sole owner of an account is blocked (`GroupHasSoleOwnersError`)
- Groups are the primary mechanism for bulk permission assignment
- Permission inheritance: User ← Group ← Role ← Permission

## Result Codes

| Code | Meaning |
|------|---------|
| `CreateGroupResultCode.Success` | Group created |
| `CreateGroupResultCode.GroupExistsError` | Duplicate name in account |
| `DeleteGroupResultCode.GroupHasSoleOwnersError` | Contains sole account owner |
