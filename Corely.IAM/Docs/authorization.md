# Authorization

Two-layer authorization model with context validation at the service level and fine-grained CRUDX permission checks at the processor level. Supports system context for headless background processes.

## Features

- **CRUDX model**: five discrete actions per resource type: Create, Read, Update, Delete, Execute
- **Exact resource types**: every permission names the one type it grants; `Guid.Empty` as the resource ID covers all resources of that type
- **Grant only what you hold**: nobody can hand out a permission, role or group membership beyond their own
- **Two authorization layers**: services validate context, processors check permissions
- **Self-ownership**: users can act on their own resources without explicit permission
- **System context**: headless processes bypass permission checks while "self" operations are blocked
- **Effective permissions**: aggregated view of permissions through roles and groups

## AuthAction Enum

```csharp
public enum AuthAction
{
    Create,
    Read,
    Update,
    Delete,
    Execute,
}
```

## IAuthorizationProvider

```csharp
public interface IAuthorizationProvider
{
    Task<bool> IsAuthorizedAsync(AuthAction action, string resourceType, params Guid[] resourceIds);
    Task<IReadOnlySet<Guid>?> GetAuthorizedResourceIdsAsync(AuthAction action, string resourceType);
    Task<bool> CanGrantAsync(string resourceType, Guid resourceId, params AuthAction[] actions);
    Task<bool> CanGrantPermissionsAsync(IEnumerable<Guid> permissionIds);
    Task<bool> CanGrantRolesAsync(IEnumerable<Guid> roleIds);
    Task<bool> CanGrantGroupAsync(Guid groupId);
    bool IsNonSystemUserContext();
    bool IsAuthorizedForOwnUser(Guid requestUserId, bool suppressLog = true);
    bool HasUserContext();
    bool HasAccountContext(Guid accountId);
}
```

| Method | Purpose |
|--------|---------|
| `IsAuthorizedAsync` | Checks CRUDX permission for specific resource types and IDs. Returns `true` for system context. |
| `GetAuthorizedResourceIdsAsync` | The resource IDs of one type the caller may act on; `null` means all of them. |
| `CanGrantAsync` and the other `CanGrant` methods | Whether the caller holds everything a grant would hand out. See [Granting](#granting). Returns `true` for system context. |
| `IsNonSystemUserContext` | Returns `true` if a real (non-system) user context is present. Used for "self" operations. |
| `IsAuthorizedForOwnUser` | Checks if the request targets the current user. Returns `false` for system context. |
| `HasUserContext` | Returns `true` if any user context is present (including system context). |
| `HasAccountContext` | Validates that the requested account ID matches the active account in the current context. Returns `true` for system context. |

## Two Authorization Layers

### Service Layer (Context Validation)

Service authorization decorators check only that the required context exists:

- **`HasUserContext()`**: user is authenticated (or system context is active)
- **`HasAccountContext(accountId)`**: user is authenticated and the requested `accountId` matches the current active account (or system context, which bypasses the account-match requirement)
- **`IsNonSystemUserContext()`**: user is a real authenticated user, NOT system context

These are coarse-grained gates. They do not check specific CRUDX permissions.

#### Category 1: "Self" Operations

Operations that only make sense for a real logged-in user (e.g., set password, manage MFA, manage Google auth, accept invitation). These use `IsNonSystemUserContext()` and block system context.

#### Category 2: "Targeting" Operations

Operations that target specific entities and can be performed by system context (e.g., register group, list users, deregister role). These use `HasAccountContext(accountId)` or `HasUserContext()`.

### Processor Layer (CRUDX Permission Checks)

Processor authorization decorators call `IsAuthorizedAsync()` with specific actions and resource types:

```csharp
// Inside a processor decorator
if (!await authorizationProvider.IsAuthorizedAsync(
    AuthAction.Create, PermissionConstants.GROUP_RESOURCE_TYPE))
{
    return new RegisterGroupResult(RegisterGroupResultCode.AuthorizationError, ...);
}
```

System context automatically passes `IsAuthorizedAsync()` checks. No permissions need to be provisioned.

## Resource Types

Permissions are scoped to resource types defined as string constants:

| Constant | Value | Description |
|----------|-------|-------------|
| `ACCOUNT_RESOURCE_TYPE` | `"account"` | Accounts |
| `USER_RESOURCE_TYPE` | `"user"` | Users |
| `GROUP_RESOURCE_TYPE` | `"group"` | Groups |
| `ROLE_RESOURCE_TYPE` | `"role"` | Roles |
| `PERMISSION_RESOURCE_TYPE` | `"permission"` | Permissions |

See [Resource Types](resource-types.md) for custom type registration and the actions an account owner gets on each type.

## All Resources of a Type

A permission always names exactly one resource type. There is no wildcard type: a permission on
`"invoice"` grants nothing on `"report"`, and `"*"` is rejected as a type name. The resource ID can
cover every resource of that type:

```csharp
// Read every group in the account
await registrationService.RegisterPermissionAsync(
    new RegisterPermissionRequest(
        accountId,
        PermissionConstants.GROUP_RESOURCE_TYPE,
        Guid.Empty,
        Read: true));
```

## Permission Resolution

When `IsAuthorizedAsync` is called:

1. Fetch all permissions for the current user (cached per account)
2. Permissions come from roles assigned directly to the user OR through groups
3. Filter by resource type (exact match)
4. Check the requested action flag (Create/Read/Update/Delete/Execute)
5. If `resourceIds` are specified, ALL must have matching permissions
6. `Guid.Empty` as a permission's resource ID grants access to all resources of that type

## Granting

Anyone who manages permissions, roles or memberships can hand out only what they already hold.
Each of these operations succeeds only if the caller holds every action it would hand out, on the
same resource type, covering the same resource ID:

| Operation | Hands out |
|-----------|-----------|
| Create a permission | The permission in the request |
| Attach permissions to a role | Those permissions |
| Assign roles to a user or a group | Every permission of those roles |
| Add users to a group | Every permission of every role of that group |

A grant is covered when, for each action it allows, the caller holds a permission of the same
type that allows that action, on `Guid.Empty` or on the grant's own resource ID. A grant on
`Guid.Empty` is covered only by the caller's own `Guid.Empty`. Actions can come from different
rows: Read from one permission and Update from another cover a grant of both.

- **All or nothing.** If any part of a request is not covered, the whole request is refused with the
  operation's `UnauthorizedError` and the message "Cannot grant permissions you do not hold".
- **System context bypasses the rule**, as it bypasses every check. It is the path for a host to
  provision access, such as a starter grant, that no user may hand out.
- **Removals are unrestricted.** Detaching a permission, removing a role or removing a member only
  reduces access.
- **Owners are bounded by their owner defaults.** An owner can hand out anything within CRUDX on the
  five IAM types and the owner actions registered for each host type.
- **Delegation is safe.** A member given `permission: C`, `role: U` and `user: U` can manage access
  without being able to exceed their own, and cannot assign the Owner role without holding
  everything it holds.
- **Existing rows are untouched.** The rule applies when a grant is made.
- **The permission cache applies**: a caller who just lost a permission can still grant it until
  their cache expires.

## Self-Ownership

`IsAuthorizedForOwnUser()` allows users to perform certain actions on their own resources (e.g., changing their own password) without explicit permission grants.

## Effective Permissions

The effective permission tree shows how permissions aggregate through the role and group hierarchy:

```
EffectivePermission
├── PermissionId, CRUDX flags, ResourceType, ResourceId
└── Roles[]
    ├── EffectiveRole (RoleId, RoleName)
    │   └── Groups[]
    │       └── EffectiveGroup (GroupId, GroupName)
    └── ...
```

Use `IRetrievalService` with `hydrate: true` to retrieve entities with their effective permission trees.

## Notes

- Service methods that appear "unguarded" are protected at the processor level where the actual work happens
- Authorization decorators are registered via Scrutor. Registration order in `ServiceRegistrationExtensions.cs` determines decorator nesting
- Permission cache is scoped to the current account and cleared on account switch
- The `IAuthorizationCacheClearer` interface is `internal`, used when roles/permissions change within a request
