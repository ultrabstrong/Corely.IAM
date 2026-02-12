using Corely.Common.Extensions;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Providers;
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
        FilterBuilder<Permission>? filter = null,
        OrderBuilder<Permission>? order = null,
        int skip = 0,
        int take = 25
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.ListPermissionsAsync(filter, order, skip, take)
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

    public async Task<RetrieveListResult<Group>> ListGroupsAsync(
        FilterBuilder<Group>? filter = null,
        OrderBuilder<Group>? order = null,
        int skip = 0,
        int take = 25
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.ListGroupsAsync(filter, order, skip, take)
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

    public async Task<RetrieveListResult<Role>> ListRolesAsync(
        FilterBuilder<Role>? filter = null,
        OrderBuilder<Role>? order = null,
        int skip = 0,
        int take = 25
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.ListRolesAsync(filter, order, skip, take)
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

    public async Task<RetrieveListResult<User>> ListUsersAsync(
        FilterBuilder<User>? filter = null,
        OrderBuilder<User>? order = null,
        int skip = 0,
        int take = 25
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.ListUsersAsync(filter, order, skip, take)
            : new RetrieveListResult<User>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to list users",
                null
            );

    public async Task<RetrieveSingleResult<User>> GetUserAsync(Guid userId, bool hydrate = false) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.GetUserAsync(userId, hydrate)
            : new RetrieveSingleResult<User>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to get user",
                default,
                null
            );

    public async Task<RetrieveListResult<Account>> ListAccountsAsync(
        FilterBuilder<Account>? filter = null,
        OrderBuilder<Account>? order = null,
        int skip = 0,
        int take = 25
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.ListAccountsAsync(filter, order, skip, take)
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
}
