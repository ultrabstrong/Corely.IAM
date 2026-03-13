# EffectivePermissionsPanel

Displays effective permissions aggregated by role, showing how permissions are derived through direct assignment and group inheritance.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Permissions` | `List<EffectivePermission>?` | — | Permissions to display |
| `UseCard` | `bool` | `true` | Wrap in card layout (vs. section divider) |

## Usage

```razor
<EffectivePermissionsPanel Permissions="@_effectivePermissions" />
```

## Display

- **Aggregated CRUDX badges** — green for granted, gray for not granted (across all permissions)
- **Per-role breakdown** — each role listed with:
    - Role name (linked to role detail page)
    - **"direct"** badge — green, shown when role is directly assigned
    - **"via GroupName"** badges — indigo, one per group the role comes through
    - **CRUDX mini-label** — monospace text showing the permission flags

## Behavior

- Aggregates CRUDX flags across all permissions (true if ANY permission grants the action)
- Groups permissions by role and tracks derivation sources
- Sorted alphabetically by role name
- Links to role detail pages via `AppRoutes.Roles/{Id}`
