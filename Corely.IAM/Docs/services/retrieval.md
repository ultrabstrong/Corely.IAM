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
| `ListApiKeysAsync` | `ListApiKeysRequest` | `RetrieveListResult<ApiKey>` |
| `GetApiKeyAsync` | `Guid apiKeyId, bool hydrate` | `RetrieveSingleResult<ApiKey>` |
| `ListRolesAsync` | `ListRolesRequest` | `RetrieveListResult<Role>` |
| `GetRoleAsync` | `Guid roleId, bool hydrate` | `RetrieveSingleResult<Role>` |
| `ListUsersAsync` | `ListUsersRequest` | `RetrieveListResult<User>` |
| `GetUserAsync` | `Guid userId, bool hydrate` | `RetrieveSingleResult<User>` |
| `ListAccountsAsync` | `ListAccountsRequest` | `RetrieveListResult<Account>` |
| `GetAccountAsync` | `Guid accountId, bool hydrate` | `RetrieveSingleResult<Account>` |
| `GetCurrentUserEffectivePermissionsAsync` | `string resourceType, Guid? resourceId` | `RetrieveSingleResult<List<EffectivePermission>>`; see below |

`GetCurrentUserEffectivePermissionsAsync` tells the signed in user their own access in the current account, for any registered resource type, including a host's own. With a `resourceId` it covers that resource and the whole type; without one, only permissions on the whole type. It reads the caller's own roles and groups, in the current account and the platform account, so it never says anything about another user or another account's rows: another account's id answers the same as an id that does not exist. It does not check that the resource exists. An API key or the system context has no user, and gets `UnauthorizedError`.

> **Note:** `GetTotpStatusAsync` and `GetAuthMethodsAsync` have been moved to dedicated services. See [MFA](../mfa.md) (`IMfaService`) and [Federated Sign-In](../federated-sign-in.md) (`IFederatedIdentityService`).

### Key Providers

| Method | Returns |
|--------|---------|
| `GetAccountSymmetricEncryptionProviderAsync(Guid)` | `RetrieveSingleResult<IIamSymmetricEncryptionProvider>` |
| `GetAccountAsymmetricEncryptionProviderAsync(Guid)` | `RetrieveSingleResult<IIamAsymmetricEncryptionProvider>` |
| `GetAccountAsymmetricSignatureProviderAsync(Guid)` | `RetrieveSingleResult<IIamAsymmetricSignatureProvider>` |
| `GetAccountJsonWebKeySetAsync(Guid)` | `RetrieveSingleResult<AccountJsonWebKeySet>`, the account's public keys, with no user context needed |
| `RevealAccountKeyAsync(RevealAccountKeyRequest)` | `RevealAccountKeyResult`; see [Key Management](../security/key-management.md#revealing-account-keys) |
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

- **Service level**: entity and account key methods require account context; user key provider methods require a non-system authenticated user context
- **Processor level**: CRUDX Read permission on the target resource type for entity/account retrieval; user key providers resolve the current user's keys only
- User key providers use the current user context (no user ID parameter)

## Notes

- All list methods return `RetrieveListResult<T>` with `PagedResult<T>` containing `Items` and `TotalCount`
- All get methods return `RetrieveSingleResult<T>` with `Data` and `ResultCode`
- Key provider methods decrypt stored keys using the system key, so the returned providers are ready to use
