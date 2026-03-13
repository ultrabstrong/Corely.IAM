# PermissionView

Authorization gate component that conditionally renders content based on the current user's CRUDX permissions. See [Authorization UI](../authorization-ui.md) for usage patterns.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Action` | `AuthAction` | — | CRUDX action to check |
| `Resource` | `string` | `""` | Resource type constant |
| `ResourceIds` | `Guid[]?` | `null` | Specific resource IDs (optional) |
| `ChildContent` | `RenderFragment?` | — | Default authorized content |
| `Authorized` | `RenderFragment?` | — | Overrides `ChildContent` when authorized |
| `NotAuthorized` | `RenderFragment?` | — | Content shown when not authorized |

## Behavior

- Calls `IAuthorizationProvider.IsAuthorizedAsync()` on parameter change
- Caches result — re-evaluation only on `Action`, `Resource`, or `ResourceIds` change
- Nothing renders until the first check completes
- `ResourceIds` equality uses span comparison for performance
