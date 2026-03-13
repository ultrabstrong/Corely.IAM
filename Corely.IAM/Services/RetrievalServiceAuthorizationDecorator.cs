using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

internal class RetrievalServiceAuthorizationDecorator(
    IRetrievalService inner,
    IAuthorizationProvider authorizationProvider
) : IRetrievalService
{
    private readonly IRetrievalService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<RetrieveListResult<Permission>> ListPermissionsAsync(
        ListPermissionsRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.ListPermissionsAsync(request)
            : new RetrieveListResult<Permission>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to list permissions",
                null
            );

    public async Task<RetrieveSingleResult<Permission>> GetPermissionAsync(
        Guid permissionId,
        bool hydrate = false
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.GetPermissionAsync(permissionId, hydrate)
            : new RetrieveSingleResult<Permission>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to get permission",
                default,
                null
            );

    public async Task<RetrieveListResult<Group>> ListGroupsAsync(ListGroupsRequest request) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.ListGroupsAsync(request)
            : new RetrieveListResult<Group>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to list groups",
                null
            );

    public async Task<RetrieveSingleResult<Group>> GetGroupAsync(
        Guid groupId,
        bool hydrate = false
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.GetGroupAsync(groupId, hydrate)
            : new RetrieveSingleResult<Group>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to get group",
                default,
                null
            );

    public async Task<RetrieveListResult<Role>> ListRolesAsync(ListRolesRequest request) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.ListRolesAsync(request)
            : new RetrieveListResult<Role>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to list roles",
                null
            );

    public async Task<RetrieveSingleResult<Role>> GetRoleAsync(Guid roleId, bool hydrate = false) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.GetRoleAsync(roleId, hydrate)
            : new RetrieveSingleResult<Role>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to get role",
                default,
                null
            );

    public async Task<RetrieveListResult<User>> ListUsersAsync(ListUsersRequest request) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.ListUsersAsync(request)
            : new RetrieveListResult<User>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to list users",
                null
            );

    public async Task<RetrieveSingleResult<User>> GetUserAsync(Guid userId, bool hydrate = false) =>
        _authorizationProvider.HasUserContext()
            ? await _inner.GetUserAsync(userId, hydrate)
            : new RetrieveSingleResult<User>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to get user",
                default,
                null
            );

    public async Task<RetrieveListResult<Account>> ListAccountsAsync(ListAccountsRequest request) =>
        _authorizationProvider.HasUserContext()
            ? await _inner.ListAccountsAsync(request)
            : new RetrieveListResult<Account>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to list accounts",
                null
            );

    public async Task<RetrieveSingleResult<Account>> GetAccountAsync(
        Guid accountId,
        bool hydrate = false
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.GetAccountAsync(accountId, hydrate)
            : new RetrieveSingleResult<Account>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to get account",
                default,
                null
            );

    public async Task<TotpStatusResult> GetTotpStatusAsync() =>
        _authorizationProvider.HasUserContext()
            ? await _inner.GetTotpStatusAsync()
            : new TotpStatusResult(
                TotpStatusResultCode.UnauthorizedError,
                "Unauthorized",
                false,
                0
            );

    public async Task<AuthMethodsResult> GetAuthMethodsAsync() =>
        _authorizationProvider.HasUserContext()
            ? await _inner.GetAuthMethodsAsync()
            : new AuthMethodsResult(
                AuthMethodsResultCode.UnauthorizedError,
                "Unauthorized",
                false,
                false,
                null
            );

    public async Task<
        RetrieveSingleResult<IIamSymmetricEncryptionProvider>
    > GetAccountSymmetricEncryptionProviderAsync(Guid accountId) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.GetAccountSymmetricEncryptionProviderAsync(accountId)
            : new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to get account encryption provider",
                default,
                null
            );

    public async Task<
        RetrieveSingleResult<IIamAsymmetricEncryptionProvider>
    > GetAccountAsymmetricEncryptionProviderAsync(Guid accountId) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.GetAccountAsymmetricEncryptionProviderAsync(accountId)
            : new RetrieveSingleResult<IIamAsymmetricEncryptionProvider>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to get account encryption provider",
                default,
                null
            );

    public async Task<
        RetrieveSingleResult<IIamAsymmetricSignatureProvider>
    > GetAccountAsymmetricSignatureProviderAsync(Guid accountId) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.GetAccountAsymmetricSignatureProviderAsync(accountId)
            : new RetrieveSingleResult<IIamAsymmetricSignatureProvider>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to get account signature provider",
                default,
                null
            );

    public async Task<
        RetrieveSingleResult<IIamSymmetricEncryptionProvider>
    > GetUserSymmetricEncryptionProviderAsync() =>
        _authorizationProvider.HasUserContext()
            ? await _inner.GetUserSymmetricEncryptionProviderAsync()
            : new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to get user encryption provider",
                default,
                null
            );

    public async Task<
        RetrieveSingleResult<IIamAsymmetricEncryptionProvider>
    > GetUserAsymmetricEncryptionProviderAsync() =>
        _authorizationProvider.HasUserContext()
            ? await _inner.GetUserAsymmetricEncryptionProviderAsync()
            : new RetrieveSingleResult<IIamAsymmetricEncryptionProvider>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to get user encryption provider",
                default,
                null
            );

    public async Task<
        RetrieveSingleResult<IIamAsymmetricSignatureProvider>
    > GetUserAsymmetricSignatureProviderAsync() =>
        _authorizationProvider.HasUserContext()
            ? await _inner.GetUserAsymmetricSignatureProviderAsync()
            : new RetrieveSingleResult<IIamAsymmetricSignatureProvider>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to get user signature provider",
                default,
                null
            );
}
