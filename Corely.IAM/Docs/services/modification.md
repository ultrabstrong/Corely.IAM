# IModificationService

Updates entity properties for accounts, users, groups, and roles.

## Methods

| Method | Parameters | Returns |
|--------|-----------|---------|
| `ModifyAccountAsync` | `UpdateAccountRequest` | `ModifyResult` |
| `ModifyUserAsync` | `UpdateUserRequest` | `ModifyResult` |
| `ModifyGroupAsync` | `UpdateGroupRequest` | `ModifyResult` |
| `ModifyRoleAsync` | `UpdateRoleRequest` | `ModifyResult` |

## Usage

```csharp
var result = await modificationService.ModifyAccountAsync(
    new UpdateAccountRequest(accountId, "New Account Name"));

if (result.ResultCode == ModifyResultCode.Success)
{
    // Account updated
}
```

```csharp
var result = await modificationService.ModifyUserAsync(
    new UpdateUserRequest(userId, "newusername", "newemail@example.com"));
```

## Authorization

- **Service level**: requires account context
- **Processor level**: CRUDX Update permission on the target resource type

## Notes

- There is no `ModifyPermissionAsync` — permissions are immutable after creation (delete and recreate instead)
- Update requests include the entity ID and the new property values
- Validation runs before the update — invalid data returns a validation error code
