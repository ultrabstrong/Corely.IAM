# Migrating to Corely.IAM 3.0

Two behavior changes need action: owners no longer get access to your own resource types unless you
say so, and nobody can hand out access they do not hold. There is no schema change.

## The resource type wildcard is gone

`PermissionConstants.ALL_RESOURCE_TYPES` (`"*"`) is removed. A permission now names exactly one
resource type, and `"*"` is rejected as a type name, by `RegisterResourceType` and by
`PermissionValidator`.

Until now every account's Owner role held one permission on `"*"`, which covered every type,
including the ones your app registers. That is how an owner could, for example, write billing grants
for their own account.

### Declare what owners get on your types

`RegisterResourceType` takes the actions an account owner gets on the type:

```csharp
// 2.x
options.RegisterResourceType("sftp", "SFTP access");

// 3.0
options.RegisterResourceType("sftp", "SFTP access", AuthAction.Read, AuthAction.Update, AuthAction.Execute);
```

**A type registered without owner actions gives owners nothing on it.** If you register types and do
not add owner actions, your owners lose access to them.

The five IAM types (`account`, `user`, `group`, `role`, `permission`) still give owners all five
actions; that is not configurable.

### What an account's Owner role holds

New accounts get one system-defined permission per registered type that has owner actions, on
`Guid.Empty`, with exactly those actions. Those rows cannot be deleted or detached from the Owner
role. Any other permission on the Owner role can be added and removed freely.

### Existing databases

Accounts created before 3.0 still hold the old `"*"` row, which now matches nothing, so their owners
lose access until the data is fixed. No migration ships with the library. For each account:

1. Replace each permission on `"*"` with explicit rows for the IAM types (and your types, if the old
   row should keep reaching them), with the same actions and role links.
2. Give the Owner role its owner defaults: one system-defined row per type with owner actions, on
   `Guid.Empty`. The permissions table is unique on account, type, resource ID and all five actions,
   so reuse a row that already exists rather than inserting a duplicate.
3. Delete the `"*"` rows.

`ResourceTypeInfo` gains `OwnerActions`, and `Permission` gains `IsSystemDefined`. `Role` gains
`SystemDefinedPermissionIds` when hydrated.

## Grant only what you hold

Each of these is now refused, with the operation's `UnauthorizedError` and the message "Cannot grant
permissions you do not hold", unless the caller already holds every action it would hand out, on the
same type, covering the same resource ID:

| Operation | Hands out |
|-----------|-----------|
| `RegisterPermissionAsync` | The permission in the request |
| `RegisterPermissionsWithRoleAsync` | Those permissions |
| `RegisterRolesWithUserAsync`, `RegisterRolesWithGroupAsync` | Every permission of those roles |
| `RegisterUsersWithGroupAsync` | Every permission of every role of that group |

System context bypasses the rule. If your app provisions access users may not hand out themselves,
a starter grant for example, do it under `IAuthenticationService.AuthenticateAsSystem()`.

`IAuthorizationProvider` gains `CanGrantAsync`, `CanGrantPermissionsAsync`, `CanGrantRolesAsync` and
`CanGrantGroupAsync`. Implementations of the interface outside the library must add them.

## The migration CLI

`corely-iam-db` 3.0 targets Corely.IAM 3.0. Its commands are unchanged.
