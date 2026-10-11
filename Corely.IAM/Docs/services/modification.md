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
| `ModifyPasswordAsync` | `ModifyPasswordRequest` | `ModifyPasswordResult` |

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

## Password Changes

A signed in session alone cannot change or remove a password. `ModifyPasswordAsync` and
`IDeregistrationService.DeregisterBasicAuthAsync` take the current password, and when the user has
TOTP enabled, a TOTP or recovery code. `IRegistrationService.RegisterPasswordAsync`, which adds a
first password, takes the code too. A recovery code is consumed, as at sign in.

```csharp
var result = await modificationService.ModifyPasswordAsync(
    new ModifyPasswordRequest(currentPassword, newPassword, totpCode));
```

A wrong current password counts toward `MaxLoginAttempts` like a failed sign in, and a locked out user
gets `UserLockedError` until `LockoutCooldownSeconds` passes.

## Authorization

- **Service level**: requires account context
- **Processor level**: CRUDX Update permission on the target resource type
- **Key rotation**: account keys need Update on the account; user keys are a self operation, so only the signed in user rotates their own and system context is refused
- **Password change**: a self operation; system context is refused

## Notes

- `ModifyPermissionAsync` changes only a permission's description, its name, and works on system-defined permissions too. A permission's type, resource ID and actions never change; delete and recreate instead
- Update requests include the entity ID and the new property values
- Validation runs before the update, and invalid data returns a validation error code
