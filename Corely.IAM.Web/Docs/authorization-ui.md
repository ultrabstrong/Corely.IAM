# Authorization in the UI

`PermissionView` is a Blazor component that shows or hides UI elements based on the current user's CRUDX permissions. It wraps any content and gates visibility on a specific action and resource type.

## Usage

```razor
<PermissionView Action="AuthAction.Create" Resource="@PermissionConstants.PERMISSION_RESOURCE_TYPE">
    <button class="btn btn-primary">Create Permission</button>
</PermissionView>
```

The button only renders if the current user has `Create` permission on the `permission` resource type.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Action` | `AuthAction` | — | CRUDX action to check |
| `Resource` | `string` | `""` | Resource type constant |
| `ResourceIds` | `Guid[]?` | `null` | Specific resource IDs to check (optional) |
| `ChildContent` | `RenderFragment?` | — | Default content shown when authorized |
| `Authorized` | `RenderFragment?` | — | Content shown when authorized (overrides `ChildContent`) |
| `NotAuthorized` | `RenderFragment?` | — | Content shown when NOT authorized |

### Authorized/NotAuthorized Fragments

Show different content based on authorization state:

```razor
<PermissionView Action="AuthAction.Delete" Resource="@PermissionConstants.ROLE_RESOURCE_TYPE">
    <Authorized>
        <button class="btn btn-danger">Delete Role</button>
    </Authorized>
    <NotAuthorized>
        <span class="text-muted">Insufficient permissions</span>
    </NotAuthorized>
</PermissionView>
```

If only `ChildContent` is provided (no `Authorized` fragment), it is used as the authorized content. If `NotAuthorized` is omitted, nothing renders for unauthorized users.

## How It Works

1. On parameter change, `PermissionView` calls `IAuthorizationProvider.IsAuthorizedAsync()`
2. The result is cached — re-evaluation only happens when `Action`, `Resource`, or `ResourceIds` change
3. Parameter equality uses span comparison for `ResourceIds` arrays (performance optimization)
4. Nothing renders until the first authorization check completes (`_checkComplete` flag)

## Common Patterns

### Gating a Button

```razor
<PermissionView Action="AuthAction.Create" Resource="@PermissionConstants.GROUP_RESOURCE_TYPE">
    <button @onclick="ShowCreateForm" class="btn btn-primary">New Group</button>
</PermissionView>
```

### Gating an Entire Section

```razor
<PermissionView Action="AuthAction.Read" Resource="@PermissionConstants.USER_RESOURCE_TYPE">
    <div class="user-list">
        @* User table content *@
    </div>
</PermissionView>
```

### Resource-Specific Gates

```razor
<PermissionView Action="AuthAction.Update"
                Resource="@PermissionConstants.ACCOUNT_RESOURCE_TYPE"
                ResourceIds="@(new[] { accountId })">
    <button @onclick="EditAccount" class="btn btn-sm btn-outline-primary">Edit</button>
</PermissionView>
```

## Notes

- `PermissionView` injects `IAuthorizationProvider` directly — it does not use the Blazor `AuthorizeView` component
- The component is in the `Corely.IAM.Web.Components.Shared` namespace
- Use `PermissionConstants` for resource type values to avoid magic strings
