# Permissions

A permission is shown by its name, its `Description`. A permission without one shows a generated
name in the same format the owner defaults use: `Resource : Actions`, for example
`Group : Full Access`, `Quota : Read & Execute` or `Invoice : Create, Read, & Update`
(`Permission.DisplayName`).

## PermissionList

**Parameters**: none. Wrap it in `AccountScope` in the shell; see [Hosting pages](../hosting-pages.md).

Permission table with search, sort, pagination, and create. CRUDX flags displayed as colored badges.

**Base class**: `EntityListPageBase<Permission>`

**Features:**
- **Search**: permission name (debounced 300ms)
- **Sort**: ascending/descending on permission name
- **Pagination**: 25 items per page
- **Create**: modal form with a resource type picker, resource ID, name, CRUDX checkboxes
- **Delete**: confirmation modal per row
- **CRUDX badges**: individual Create, Read, Update, Delete, Execute columns with active/inactive indicators

**Authorization gates:**
- `AuthAction.Create` + `Permission.RESOURCE_TYPE`: Create button
- `AuthAction.Delete` + `Permission.RESOURCE_TYPE` + `ResourceIds: [permissionId]`: Delete button per row
- System-defined permissions (the Owner role's defaults) have no Delete button

**Create form:**
- **Resource Type**: a [ResourceTypePicker](resource-type-picker.md) of the types the caller can
  grant at least one action on (`IAuthorizationProvider.GetGrantableActionsAsync`), grouped and
  searchable
- **Resource ID**: text input for GUID; empty defaults to `Guid.Empty` (all resources of the type)
- **Name**: optional; left empty, the permission shows its generated name
- **CRUDX checkboxes**: an action the caller does not hold on that type and resource ID is disabled
- The server still refuses a permission the caller does not hold, with "Cannot grant permissions you
  do not hold"

**Table columns:**
- Permission: the name, then a pill with the resource type and, for an owner default, a System pill
- Resource ID (`"all"` displayed for `Guid.Empty`)
- C, R, U, D, X, each as a colored badge

---

## PermissionDetail

**Parameters**: `Id` (`Guid`, required), the permission. Wrap it in `AccountScope` in the shell; see [Hosting pages](../hosting-pages.md).

**Base class**: `EntityDetailPageBase`

**Features:**
- **Properties**: name, resource type pill (with a System pill for an owner default), resource ID
- **Edit**: changes the name only, `IModificationService.ModifyPermissionAsync`. The resource type,
  resource ID and CRUDX of a permission never change; delete and recreate for that
- **CRUDX badges**: color-coded active/inactive
- **Roles section**: the roles that hold the permission, paginated (10 per page), add via
  `EntityPickerModal` and remove per row. Add to Roles shows only when the caller could grant the
  permission (`CanGrantPermissionsAsync`), and the picker offers only roles the caller can update
- **Effective permissions panel**: shows the permission tree
- **Delete**: confirmation modal

**Authorization gates:**
- `AuthAction.Update` + `Permission.RESOURCE_TYPE` + `ResourceIds: [Id]`: Edit button, on system-defined
  permissions too
- `AuthAction.Delete` + `Permission.RESOURCE_TYPE` + `ResourceIds: [Id]`: Delete button, hidden for a
  system-defined permission
- `AuthAction.Read` + `Role.RESOURCE_TYPE`: Roles section
- `AuthAction.Update` + `Role.RESOURCE_TYPE`: Add to Roles
- `AuthAction.Update` + `Role.RESOURCE_TYPE` + `ResourceIds: [roleId]`: Remove on that row

**Behavior:**
- `Guid.Empty` resource ID displays as `"all"`
- Saving an empty name clears it, and the generated name shows instead
- Loads with `hydrate: true` for roles
- Adds to roles through `RegisterPermissionsWithRoleAsync()`, one role at a time. If one is refused the
  page stops there and says how many were added
- A system-defined permission on the Owner role shows a System badge instead of Remove
- Delete navigates to the host's `IIamWebLinks.Permissions`
