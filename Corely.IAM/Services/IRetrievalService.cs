using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

public interface IRetrievalService
{
    Task<RetrieveListResult<Permission>> ListPermissionsAsync(ListPermissionsRequest request);

    Task<RetrieveSingleResult<Permission>> GetPermissionAsync(
        Guid permissionId,
        bool hydrate = false
    );

    Task<RetrieveListResult<Group>> ListGroupsAsync(ListGroupsRequest request);

    Task<RetrieveSingleResult<Group>> GetGroupAsync(Guid groupId, bool hydrate = false);

    Task<RetrieveListResult<Role>> ListRolesAsync(ListRolesRequest request);

    Task<RetrieveSingleResult<Role>> GetRoleAsync(Guid roleId, bool hydrate = false);

    Task<RetrieveListResult<User>> ListUsersAsync(ListUsersRequest request);

    Task<RetrieveSingleResult<User>> GetUserAsync(Guid userId, bool hydrate = false);

    Task<RetrieveListResult<Account>> ListAccountsAsync(ListAccountsRequest request);

    Task<RetrieveSingleResult<Account>> GetAccountAsync(Guid accountId, bool hydrate = false);

    Task<
        RetrieveSingleResult<IIamSymmetricEncryptionProvider>
    > GetAccountSymmetricEncryptionProviderAsync(Guid accountId);

    Task<
        RetrieveSingleResult<IIamAsymmetricEncryptionProvider>
    > GetAccountAsymmetricEncryptionProviderAsync(Guid accountId);

    Task<
        RetrieveSingleResult<IIamAsymmetricSignatureProvider>
    > GetAccountAsymmetricSignatureProviderAsync(Guid accountId);

    Task<
        RetrieveSingleResult<IIamSymmetricEncryptionProvider>
    > GetUserSymmetricEncryptionProviderAsync();

    Task<
        RetrieveSingleResult<IIamAsymmetricEncryptionProvider>
    > GetUserAsymmetricEncryptionProviderAsync();

    Task<
        RetrieveSingleResult<IIamAsymmetricSignatureProvider>
    > GetUserAsymmetricSignatureProviderAsync();
}
