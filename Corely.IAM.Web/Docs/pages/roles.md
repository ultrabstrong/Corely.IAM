# Roles

## RoleList (`/roles`)

Role table with search, sort, pagination, and create/delete operations. System-defined roles are distinguished with a badge and cannot be deleted.

**Base class**: `EntityListPageBase<Role>`

**Features:**
- **Search**: name, description (debounced 300ms)
- **Sort**: ascending/descending on name or description
- **Pagination**: 25 items per page
- **Create**: modal form with role name field
- **Delete**: confirmation modal (hidden for system roles)
- **Role type badge**: "System" (gray) or "User" (light) per row

**Authorization gates:**
- `AuthAction.Create` + `ROLE_RESOURCE_TYPE`: Create button
- `AuthAction.Delete` + `ROLE_RESOURCE_TYPE` + `ResourceIds: [roleId]`: Delete button (conditionally hidden for system roles)

---

## RoleDetail (`/roles/{Id:guid}`)

Role properties with permission assignment. A system-defined role's name and description are read-only; its permissions can still be added and removed.

**Base class**: `EntityDetailPageBase`

**Features:**
- **Edit properties**: name, description (disabled for system roles)
- **Permissions section**: paginated (10 per page), add/remove via `EntityPickerModal`
- **Effective permissions panel**: all permissions assigned to this role
- **System role badge**: displayed for system-defined roles

**Authorization gates:**
- `AuthAction.Update` + `ROLE_RESOURCE_TYPE` + `ResourceIds: [Id]`: Edit (hidden for system roles), Add Permissions, Remove Permission
- `AuthAction.Delete` + `ROLE_RESOURCE_TYPE` + `ResourceIds: [Id]`: Delete button (hidden for system roles)

**Behavior:**
- Edit/delete disabled when `IsSystemDefined = true`
- Loads with `hydrate: true` for permissions relation
- Uses `RegisterPermissionsWithRoleAsync()` for bulk permission assignment
- On the Owner role, a system-defined permission (one of the owner defaults) shows a System badge instead of Remove; `ChildRef.IsSystemDefined` says which
- Delete redirects to `/roles`
