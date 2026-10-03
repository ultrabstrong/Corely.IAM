# IModificationService

Updates entity properties for accounts, users, groups, roles and permissions, and rotates account and user keys.

## Methods

| Method | Parameters | Returns |
|--------|-----------|---------|
| `ModifyAccountAsync` | `UpdateAccountRequest` | `ModifyResult` |
| `ModifyUserAsync` | `UpdateUserRequest` | `ModifyResult` |
| `ModifyGroupAsync` | `UpdateGroupRequest` | `ModifyResult` |
| `ModifyRoleAsync` | `UpdateRoleRequest` | `ModifyResult` |
| `ModifyPermissionAsync` | `UpdatePermissionRequest` | `ModifyResult` |
| `RotateAccountKeyAsync` | `RotateAccountKeyRequest` | `ModifyResult` |
| `RotateCurrentUserKeyAsync` | `KeyType` | `ModifyResult` |

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

```csharp
var result = await modificationService.RotateAccountKeyAsync(
    new RotateAccountKeyRequest(accountId, KeyType.AsymmetricSignature));
```

Rotation is covered in [Key Management](../security/key-management.md#rotation).

## Authorization

- **Service level**: requires account context
- **Processor level**: CRUDX Update permission on the target resource type
- **Key rotation**: account keys need Update on the account; user keys are a self operation, so only the signed in user rotates their own and system context is refused

## Notes

- `ModifyPermissionAsync` changes only a permission's description, its name, and works on system-defined permissions too. A permission's type, resource ID and actions never change; delete and recreate instead
- Update requests include the entity ID and the new property values
- Validation runs before the update, and invalid data returns a validation error code
