# Groups

## GroupList — `/groups`

Group table with search, sort, pagination, and create/delete operations.

**Base class**: `EntityListPageBase<Group>`

**Features:**
- **Search** — name, description (debounced 300ms)
- **Sort** — ascending/descending on name or description
- **Pagination** — 25 items per page
- **Create** — modal form with group name field
- **Delete** — confirmation modal per row

**Authorization gates:**
- `AuthAction.Create` + `GROUP_RESOURCE_TYPE` — Create button
- `AuthAction.Delete` + `GROUP_RESOURCE_TYPE` + `ResourceIds: [groupId]` — Delete button per row

---

## GroupDetail — `/groups/{Id:guid}`

Group properties with user and role management.

**Base class**: `EntityDetailPageBase`

**Features:**
- **Edit properties** — name, description (toggleable edit mode with cancel support)
- **Users section** — paginated (10 per page), add/remove via `EntityPickerModal`
- **Roles section** — paginated (10 per page), add/remove via `EntityPickerModal`
- **Effective permissions panel** — inherited permissions through assigned roles

**Authorization gates:**
- `AuthAction.Update` + `GROUP_RESOURCE_TYPE` + `ResourceIds: [Id]` — Edit, Add Users, Add Roles, Remove User, Remove Role
- `AuthAction.Delete` + `GROUP_RESOURCE_TYPE` + `ResourceIds: [Id]` — Delete button

**Behavior:**
- Loads with `hydrate: true` for user and role relations
- Edit mode stores original values for cancel functionality
- Delete redirects to `/groups`
