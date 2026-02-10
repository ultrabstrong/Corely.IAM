using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Ordering;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class RetrievalService(ILogger<RetrievalService> logger) : IRetrievalService
{
    private readonly ILogger<RetrievalService> _logger = logger.ThrowIfNull(nameof(logger));

    public Task<RetrieveListResult<Permission>> ListPermissionsAsync(
        FilterBuilder<Permission>? filter = null,
        OrderBuilder<Permission>? order = null,
        int skip = 0,
        int take = 25
    ) => throw new NotImplementedException();

    public Task<RetrieveSingleResult<Permission>> GetPermissionAsync(
        Guid permissionId,
        bool hydrate = false
    ) => throw new NotImplementedException();

    public Task<RetrieveListResult<Group>> ListGroupsAsync(
        FilterBuilder<Group>? filter = null,
        OrderBuilder<Group>? order = null,
        int skip = 0,
        int take = 25
    ) => throw new NotImplementedException();

    public Task<RetrieveSingleResult<Group>> GetGroupAsync(Guid groupId, bool hydrate = false) =>
        throw new NotImplementedException();

    public Task<RetrieveListResult<Role>> ListRolesAsync(
        FilterBuilder<Role>? filter = null,
        OrderBuilder<Role>? order = null,
        int skip = 0,
        int take = 25
    ) => throw new NotImplementedException();

    public Task<RetrieveSingleResult<Role>> GetRoleAsync(Guid roleId, bool hydrate = false) =>
        throw new NotImplementedException();

    public Task<RetrieveListResult<User>> ListUsersAsync(
        FilterBuilder<User>? filter = null,
        OrderBuilder<User>? order = null,
        int skip = 0,
        int take = 25
    ) => throw new NotImplementedException();

    public Task<RetrieveSingleResult<User>> GetUserAsync(Guid userId, bool hydrate = false) =>
        throw new NotImplementedException();

    public Task<RetrieveListResult<Account>> ListAccountsAsync(
        FilterBuilder<Account>? filter = null,
        OrderBuilder<Account>? order = null,
        int skip = 0,
        int take = 25
    ) => throw new NotImplementedException();

    public Task<RetrieveSingleResult<Account>> GetAccountAsync(
        Guid accountId,
        bool hydrate = false
    ) => throw new NotImplementedException();
}
