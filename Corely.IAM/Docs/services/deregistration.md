# IDeregistrationService

Deletes entities and removes relationships between entities.

## Methods

| Method | Parameters | Returns |
|--------|-----------|---------|
| `DeregisterUserAsync` | *(none — uses current user context)* | `DeregisterUserResult` |
| `DeregisterAccountAsync` | `DeregisterAccountRequest` | `DeregisterAccountResult` |
| `DeregisterGroupAsync` | `DeregisterGroupRequest` | `DeregisterGroupResult` |
| `DeregisterRoleAsync` | `DeregisterRoleRequest` | `DeregisterRoleResult` |
| `DeregisterPermissionAsync` | `DeregisterPermissionRequest` | `DeregisterPermissionResult` |
| `DeregisterUserFromAccountAsync` | `DeregisterUserFromAccountRequest` | `DeregisterUserFromAccountResult` |
| `DeregisterUsersFromGroupAsync` | `DeregisterUsersFromGroupRequest` | `DeregisterUsersFromGroupResult` |
| `DeregisterRolesFromGroupAsync` | `DeregisterRolesFromGroupRequest` | `DeregisterRolesFromGroupResult` |
| `DeregisterRolesFromUserAsync` | `DeregisterRolesFromUserRequest` | `DeregisterRolesFromUserResult` |
| `DeregisterPermissionsFromRoleAsync` | `DeregisterPermissionsFromRoleRequest` | `DeregisterPermissionsFromRoleResult` |
| `DeregisterBasicAuthAsync` | *(none — uses current user context)* | `DeregisterBasicAuthResult` |

> **Note:** `UnlinkGoogleAuthAsync` has been moved to `IGoogleAuthService`. See [Google Sign-In](../google-signin.md).

## Usage

### Delete the Current User

```csharp
var result = await deregistrationService.DeregisterUserAsync();
```

Operates on the authenticated user — no user ID parameter needed.

### Delete the Current Account

```csharp
var result = await deregistrationService.DeregisterAccountAsync(
    new DeregisterAccountRequest(accountId));
```

Deletes the specified account after service-level account-context validation.

### Remove Users from a Group

```csharp
var result = await deregistrationService.DeregisterUsersFromGroupAsync(
    new DeregisterUsersFromGroupRequest(groupId, [userId1, userId2]));
```

### Remove a User from an Account

```csharp
var result = await deregistrationService.DeregisterUserFromAccountAsync(
    new DeregisterUserFromAccountRequest(userId, accountId));
```

Removes the specified user from the specified account. The same API supports both self-service removal and admin removal — self-removal passes the current user's ID, while admin removal targets another user in the account.

## Authorization

- **Service level**: requires user context for user deletion; requires account context for account-scoped relationship methods; `DeregisterUserFromAccountAsync` also allows the current user to remove themselves
- **Processor level**: CRUDX Delete permission checks on the target resource type

## Notes

- Entity deletion manually clears M:M relationship collections before deleting (SQL Server constraint: no cascade deletes on M:M)
- `DeregisterUserAsync` and `DeregisterAccountAsync` use the current context — they cannot target arbitrary users/accounts
- `DeregisterUserFromAccountAsync` is the single account-removal API for both self-service and admin flows
