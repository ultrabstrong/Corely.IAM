# IRetrievalService

Queries entities with filtering, ordering, and pagination. Also retrieves encryption/signature key providers for accounts and users.

## Methods

### Entity Queries

| Method | Parameters | Returns |
|--------|-----------|---------|
| `ListPermissionsAsync` | `ListPermissionsRequest` | `RetrieveListResult<Permission>` |
| `GetPermissionAsync` | `Guid permissionId, bool hydrate` | `RetrieveSingleResult<Permission>` |
| `ListGroupsAsync` | `ListGroupsRequest` | `RetrieveListResult<Group>` |
| `GetGroupAsync` | `Guid groupId, bool hydrate` | `RetrieveSingleResult<Group>` |
| `ListRolesAsync` | `ListRolesRequest` | `RetrieveListResult<Role>` |
| `GetRoleAsync` | `Guid roleId, bool hydrate` | `RetrieveSingleResult<Role>` |
| `ListUsersAsync` | `ListUsersRequest` | `RetrieveListResult<User>` |
| `GetUserAsync` | `Guid userId, bool hydrate` | `RetrieveSingleResult<User>` |
| `ListAccountsAsync` | `ListAccountsRequest` | `RetrieveListResult<Account>` |
| `GetAccountAsync` | `Guid accountId, bool hydrate` | `RetrieveSingleResult<Account>` |

### Key Providers

| Method | Returns |
|--------|---------|
| `GetAccountSymmetricEncryptionProviderAsync(Guid)` | `RetrieveSingleResult<IIamSymmetricEncryptionProvider>` |
| `GetAccountAsymmetricEncryptionProviderAsync(Guid)` | `RetrieveSingleResult<IIamAsymmetricEncryptionProvider>` |
| `GetAccountAsymmetricSignatureProviderAsync(Guid)` | `RetrieveSingleResult<IIamAsymmetricSignatureProvider>` |
| `GetUserSymmetricEncryptionProviderAsync()` | `RetrieveSingleResult<IIamSymmetricEncryptionProvider>` |
| `GetUserAsymmetricEncryptionProviderAsync()` | `RetrieveSingleResult<IIamAsymmetricEncryptionProvider>` |
| `GetUserAsymmetricSignatureProviderAsync()` | `RetrieveSingleResult<IIamAsymmetricSignatureProvider>` |

## Usage

### List with Filtering and Pagination

```csharp
var filter = Filter.For<User>()
    .Where(u => u.Username, StringFilter.Contains("john"));

var order = Order.For<User>()
    .By(u => u.Username, SortDirection.Ascending);

var result = await retrievalService.ListUsersAsync(
    new ListUsersRequest(filter, order, skip: 0, take: 25));

var users = result.Data?.Items;
var totalCount = result.Data?.TotalCount;
```

### Get with Hydration

```csharp
var result = await retrievalService.GetRoleAsync(roleId, hydrate: true);
```

When `hydrate` is `true`, related entities are included (e.g., a role's permissions and groups). Without hydration, only the entity's own properties are populated.

### Get Encryption Provider

```csharp
var result = await retrievalService.GetAccountSymmetricEncryptionProviderAsync(accountId);
if (result.ResultCode == RetrieveResultCode.Success)
{
    var provider = result.Data;
    var encrypted = provider.Encrypt(plaintext);
}
```

## Filtering and Ordering

List requests accept `FilterBuilder<T>` and `OrderBuilder<T>` from `Corely.Common`. See the [Corely.Common filtering docs](https://github.com/ultrabstrong/Corely/tree/master/Corely.Common/Docs) for the full API.

## Authorization

- **Service level**: requires account context for all methods
- **Processor level**: CRUDX Read permission on the target resource type
- User key providers use the current user context (no user ID parameter)

## Notes

- All list methods return `RetrieveListResult<T>` with `PagedResult<T>` containing `Items` and `TotalCount`
- All get methods return `RetrieveSingleResult<T>` with `Data` and `ResultCode`
- Key provider methods decrypt stored keys using the system key — the returned providers are ready to use
