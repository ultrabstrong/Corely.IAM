# API Keys

## ApiKeyList

**Parameters**: none. Wrap it in `AccountScope` in the shell; see [Hosting pages](../hosting-pages.md).

Key table with search, sort, pagination, and create/delete operations. When the platform's security
settings leave keys off for the account, the page says so and lists nothing.

**Base class**: `EntityListPageBase<ApiKey>`

**Features:**
- **Search**: name
- **Sort**: name
- **Create**: modal form with name, description and lifetime in days, no longer than
  `ApiKeyMaxLifetimeDays`. The new key is shown once in `ApiKeySecretModal`, with a copy button
- **Delete**: confirmation modal
- **Locked badge**: shown while repeated wrong secrets have locked the key

**Authorization gates:**
- `AuthAction.Create` + `ApiKey.RESOURCE_TYPE`: Create button
- `AuthAction.Delete` + `ApiKey.RESOURCE_TYPE` + `ResourceIds: [apiKeyId]`: Delete button

---

## ApiKeyDetail

**Parameters**: `Id` (`Guid`, required), the key. Wrap it in `AccountScope` in the shell; see [Hosting pages](../hosting-pages.md).

Key properties, its two secrets, and its roles and groups.

**Base class**: `EntityDetailPageBase`

**Features:**
- **Edit properties**: name, description
- **Secrets**: the primary and secondary slot with prefix, expiry and last use. Issue or rotate either
  slot; the new key is shown once in `ApiKeySecretModal`
- **Roles and groups**: add and remove, with pickers that offer only roles and groups the caller could
  grant
- **Allowed types**: the resource types the platform lets keys act on

**Authorization gates:**
- `AuthAction.Update` + `ApiKey.RESOURCE_TYPE` + `ResourceIds: [Id]`: Edit, rotate, add and remove roles and groups
- `AuthAction.Delete` + `ApiKey.RESOURCE_TYPE` + `ResourceIds: [Id]`: Delete button

**Behavior:**
- Loads with `hydrate: true` for roles and groups
- Adds groups through `RegisterApiKeysWithGroupAsync()`, one group at a time
- Delete navigates to the host's `IIamWebLinks.ApiKeys`

Group pages list a group's keys beside its users, and role pages list the users, groups and keys that
hold the role. Both hide keys while the platform leaves them off for the account.
