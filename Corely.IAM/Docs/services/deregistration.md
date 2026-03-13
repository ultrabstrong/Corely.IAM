# IDeregistrationService

Deletes entities and removes relationships between entities.

## Methods

| Method | Parameters | Returns |
|--------|-----------|---------|
| `DeregisterUserAsync` | *(none — uses current user context)* | `DeregisterUserResult` |
| `DeregisterAccountAsync` | *(none — uses current account context)* | `DeregisterAccountResult` |
| `DeregisterGroupAsync` | `DeregisterGroupRequest` | `DeregisterGroupResult` |
| `DeregisterRoleAsync` | `DeregisterRoleRequest` | `DeregisterRoleResult` |
| `DeregisterPermissionAsync` | `DeregisterPermissionRequest` | `DeregisterPermissionResult` |
| `DeregisterUserFromAccountAsync` | `DeregisterUserFromAccountRequest` | `DeregisterUserFromAccountResult` |
| `LeaveAccountAsync` | `Guid accountId` | `DeregisterUserFromAccountResult` |
| `DeregisterUsersFromGroupAsync` | `DeregisterUsersFromGroupRequest` | `DeregisterUsersFromGroupResult` |
| `DeregisterRolesFromGroupAsync` | `DeregisterRolesFromGroupRequest` | `DeregisterRolesFromGroupResult` |
| `DeregisterRolesFromUserAsync` | `DeregisterRolesFromUserRequest` | `DeregisterRolesFromUserResult` |
| `DeregisterPermissionsFromRoleAsync` | `DeregisterPermissionsFromRoleRequest` | `DeregisterPermissionsFromRoleResult` |

## Usage

### Delete the Current User

```csharp
var result = await deregistrationService.DeregisterUserAsync();
```

Operates on the authenticated user — no user ID parameter needed.

### Delete the Current Account

```csharp
var result = await deregistrationService.DeregisterAccountAsync();
```

Operates on the current account context.

### Remove Users from a Group

```csharp
var result = await deregistrationService.DeregisterUsersFromGroupAsync(
    new DeregisterUsersFromGroupRequest(groupId, [userId1, userId2]));
```

### Leave an Account

```csharp
var result = await deregistrationService.LeaveAccountAsync(accountId);
```

Removes the current user from the specified account. Different from `DeregisterUserFromAccountAsync` which removes another user (requires admin permissions).

## Authorization

- **Service level**: requires user context for user/account deletion; requires account context for relationship methods
- **Processor level**: CRUDX Delete permission checks on the target resource type

## Notes

- Entity deletion manually clears M:M relationship collections before deleting (SQL Server constraint: no cascade deletes on M:M)
- `DeregisterUserAsync` and `DeregisterAccountAsync` use the current context — they cannot target arbitrary users/accounts
- `LeaveAccountAsync` is the self-service variant; `DeregisterUserFromAccountAsync` is the admin variant
