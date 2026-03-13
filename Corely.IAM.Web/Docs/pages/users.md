# Users

## UserList — `/users`

User table for the current account with search, sort, and pagination.

**Base class**: `EntityListPageBase<User>`

**Features:**
- **Search** — username and email (debounced 300ms)
- **Sort** — ascending/descending on username or email
- **Pagination** — 25 items per page
- **Remove** — removes user from current account (not a user delete)

**Authorization gates:**
- `AuthAction.Update` + `ACCOUNT_RESOURCE_TYPE` — Remove button

**Behavior:**
- Displays only users in the current account
- Removing the logged-in user redirects to `/select-account` with a full page reload
- Uses `DeregisterUserFromAccountAsync()` (account relationship removal, not user deletion)

---

## UserDetail — `/users/{Id:guid}`

Read-only user properties with group and role assignment.

**Base class**: `EntityDetailPageBase`

**Features:**
- **Properties** — username, email (read-only)
- **Groups section** — paginated (10 per page), add via `EntityPickerModal`
- **Roles section** — paginated (10 per page), add/remove via `EntityPickerModal`
- **Effective permissions panel** — inherited permissions through roles and groups

**Authorization gates:**
- `AuthAction.Update` + `USER_RESOURCE_TYPE` + `ResourceIds: [Id]` — Add Groups, Add Roles, Remove Role buttons

**Behavior:**
- Loads with `hydrate: true` to include group and role relations
- Role assignment uses bulk `RegisterRolesWithUserAsync()`
- Group assignment loops over selected groups individually
- Groups are add-only (cannot remove from this page)
