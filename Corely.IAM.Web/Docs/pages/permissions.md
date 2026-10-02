# Permissions

## PermissionList (`/permissions`)

Permission table with search, sort, pagination, and create. CRUDX flags displayed as colored badges.

**Base class**: `EntityListPageBase<Permission>`

**Features:**
- **Search**: resource type name (debounced 300ms)
- **Sort**: ascending/descending on resource type
- **Pagination**: 25 items per page
- **Create**: modal form with resource type dropdown, resource ID, description, CRUDX checkboxes
- **Delete**: confirmation modal per row
- **CRUDX badges**: individual Create, Read, Update, Delete, Execute columns with active/inactive indicators

**Authorization gates:**
- `AuthAction.Create` + `PERMISSION_RESOURCE_TYPE`: Create button
- `AuthAction.Delete` + `PERMISSION_RESOURCE_TYPE` + `ResourceIds: [permissionId]`: Delete button per row
- System-defined permissions (the Owner role's defaults) show a System badge instead of Delete

**Create form:**
- **Resource Type**: `<select>` dropdown populated from `IResourceTypeRegistry`
- **Resource ID**: text input for GUID; empty defaults to `Guid.Empty` (all resources of the type)
- Creating a permission the caller does not hold shows the refusal "Cannot grant permissions you do not hold"
- **Description**: auto-populated from registry when resource type is selected (editable)
- **CRUDX checkboxes**: five individual checkboxes

**Table columns:**
- Resource Type (with inline description in muted text)
- Resource ID (`"all"` displayed for `Guid.Empty`)
- C, R, U, D, X, each as a colored badge

---

## PermissionDetail (`/permissions/{Id:guid}`)

Read-only permission detail. Permissions are immutable after creation.

**Base class**: `EntityDetailPageBase`

**Features:**
- **Properties**: resource type, resource ID, description (all read-only)
- **CRUDX badges**: color-coded active/inactive
- **Effective permissions panel**: shows the permission tree
- **Delete**: confirmation modal

**Authorization gates:**
- `AuthAction.Delete` + `PERMISSION_RESOURCE_TYPE` + `ResourceIds: [Id]`: Delete button
- A system-defined permission shows a System badge and no Delete button

**Behavior:**
- No edit capability, because permissions are immutable (delete and recreate)
- `Guid.Empty` resource ID displays as `"all"`
- Delete redirects to `/permissions`
