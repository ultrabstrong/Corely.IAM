# Authorization

Two-layer authorization model with context validation at the service level and fine-grained CRUDX permission checks at the processor level. Supports system context for headless background processes.

## Features

- **CRUDX model** — five discrete actions per resource type: Create, Read, Update, Delete, Execute
- **Wildcard support** — `"*"` matches all resource types; `Guid.Empty` matches all resources of a type
- **Two authorization layers** — services validate context, processors check permissions
- **Self-ownership** — users can act on their own resources without explicit permission
- **System context** — headless processes bypass permission checks while "self" operations are blocked
- **Effective permissions** — aggregated view of permissions through roles and groups

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
    bool IsNonSystemUserContext();
    bool IsAuthorizedForOwnUser(Guid requestUserId, bool suppressLog = true);
    bool HasUserContext();
    bool HasAccountContext(Guid accountId);
}
```

| Method | Purpose |
|--------|---------|
| `IsAuthorizedAsync` | Checks CRUDX permission for specific resource types and IDs. Returns `true` for system context. |
| `IsNonSystemUserContext` | Returns `true` if a real (non-system) user context is present. Used for "self" operations. |
| `IsAuthorizedForOwnUser` | Checks if the request targets the current user. Returns `false` for system context. |
| `HasUserContext` | Returns `true` if any user context is present (including system context). |
| `HasAccountContext` | Validates that the requested account ID matches the active account in the current context. Returns `true` for system context. |

## Two Authorization Layers

### Service Layer (Context Validation)

Service authorization decorators check only that the required context exists:

- **`HasUserContext()`** — user is authenticated (or system context is active)
- **`HasAccountContext(accountId)`** — user is authenticated and the requested `accountId` matches the current active account (or system context, which bypasses the account-match requirement)
- **`IsNonSystemUserContext()`** — user is a real authenticated user, NOT system context

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

System context automatically passes `IsAuthorizedAsync()` checks — no permissions need to be provisioned.

## Resource Types

Permissions are scoped to resource types defined as string constants:

| Constant | Value | Description |
|----------|-------|-------------|
| `ACCOUNT_RESOURCE_TYPE` | `"account"` | Accounts |
| `USER_RESOURCE_TYPE` | `"user"` | Users |
| `GROUP_RESOURCE_TYPE` | `"group"` | Groups |
| `ROLE_RESOURCE_TYPE` | `"role"` | Roles |
| `PERMISSION_RESOURCE_TYPE` | `"permission"` | Permissions |
| `ALL_RESOURCE_TYPES` | `"*"` | Wildcard — all resource types |

See [Resource Types](resource-types.md) for custom type registration.

## Wildcard Permissions

Two levels of wildcard:

- **Resource type wildcard** (`"*"`) — grants access to all resource types for the specified action
- **Resource ID wildcard** (`Guid.Empty`) — grants access to all resources of the specified type

```csharp
// Permission with resource type "*" and ID Guid.Empty = full admin access for that action
await registrationService.RegisterPermissionAsync(
    new RegisterPermissionRequest(
        resourceType: PermissionConstants.ALL_RESOURCE_TYPES,
        resourceId: Guid.Empty,
        create: true, read: true, update: true, delete: true, execute: true,
        description: "Full admin"));
```

## Permission Resolution

When `IsAuthorizedAsync` is called:

1. Fetch all permissions for the current user (cached per account)
2. Permissions come from roles assigned directly to the user OR through groups
3. Filter by resource type (exact match OR wildcard `"*"`)
4. Check the requested action flag (Create/Read/Update/Delete/Execute)
5. If `resourceIds` are specified, ALL must have matching permissions
6. `Guid.Empty` as a permission's resource ID grants access to all resources of that type

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
- Authorization decorators are registered via Scrutor — registration order in `ServiceRegistrationExtensions.cs` determines decorator nesting
- Permission cache is scoped to the current account and cleared on account switch
- The `IAuthorizationCacheClearer` interface is `internal` — used when roles/permissions change within a request
