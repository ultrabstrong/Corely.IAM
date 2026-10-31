# Roles

## RoleList

**Parameters**: none. Wrap it in `AccountScope` in the shell; see [Hosting pages](../hosting-pages.md).

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
- `AuthAction.Create` + `Role.RESOURCE_TYPE`: Create button
- `AuthAction.Delete` + `Role.RESOURCE_TYPE` + `ResourceIds: [roleId]`: Delete button (conditionally hidden for system roles)

---

## RoleDetail

**Parameters**: `Id` (`Guid`, required), the role. Wrap it in `AccountScope` in the shell; see [Hosting pages](../hosting-pages.md).

Role properties with permission assignment and the users, groups and API keys that hold the role. A system-defined role's name and description are read-only; its permissions and holders can still be added and removed.

**Base class**: `EntityDetailPageBase`

**Features:**
- **Edit properties**: name, description (disabled for system roles)
- **Permissions section**: paginated (10 per page), add/remove via `EntityPickerModal`. The picker lists permissions by name with their type, and offers only permissions the caller could grant (`GetGrantablePermissionIdsAsync`)
- **Holders**: the users, groups and API keys that hold the role, each linking to its page, with add via `EntityPickerModal` and remove per row. Add shows only when the caller could grant the role (`CanGrantRolesAsync`), and each picker offers only holders the caller can update. API keys are hidden while the platform leaves keys off for the account or the caller lacks Read on `api_key`
- **Effective permissions panel**: all permissions assigned to this role
- **System role badge**: displayed for system-defined roles

**Authorization gates:**
- `AuthAction.Update` + `Role.RESOURCE_TYPE` + `ResourceIds: [Id]`: Edit (hidden for system roles), Add Permissions, Remove Permission
- `AuthAction.Update` + `User.RESOURCE_TYPE`, `Group.RESOURCE_TYPE` or `ApiKey.RESOURCE_TYPE`: Add Users, Add Groups, Add API keys. These change the holder, so they take the holder's permission rather than the role's
- `AuthAction.Update` + the holder's type + `ResourceIds: [holderId]`: Remove on that row
- `AuthAction.Delete` + `Role.RESOURCE_TYPE` + `ResourceIds: [Id]`: Delete button (hidden for system roles)

**Behavior:**
- Edit/delete disabled when `IsSystemDefined = true`
- Loads with `hydrate: true` for permissions and holders
- Uses `RegisterPermissionsWithRoleAsync()` for bulk permission assignment
- Adds holders through `RegisterRolesWithUserAsync()`, `RegisterRolesWithGroupAsync()` and `RegisterRolesWithApiKeyAsync()`, one holder at a time. If one is refused the page stops there and says how many were added
- Removing the Owner role from its sole owner is refused by the service, and the page shows why
- On the Owner role, a system-defined permission (one of the owner defaults) shows a System badge instead of Remove; `ChildRef.IsSystemDefined` says which
- Delete navigates to the host's `IIamWebLinks.Roles`
