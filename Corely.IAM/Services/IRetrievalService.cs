using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

public interface IRetrievalService
{
    Task<RetrieveListResult<Permission>> ListPermissionsAsync(
        FilterBuilder<Permission>? filter = null,
        OrderBuilder<Permission>? order = null,
        int skip = 0,
        int take = 25
    );

    Task<RetrieveSingleResult<Permission>> GetPermissionAsync(
        Guid permissionId,
        bool hydrate = false
    );

    Task<RetrieveListResult<Group>> ListGroupsAsync(
        FilterBuilder<Group>? filter = null,
        OrderBuilder<Group>? order = null,
        int skip = 0,
        int take = 25
    );

    Task<RetrieveSingleResult<Group>> GetGroupAsync(Guid groupId, bool hydrate = false);

    Task<RetrieveListResult<Role>> ListRolesAsync(
        FilterBuilder<Role>? filter = null,
        OrderBuilder<Role>? order = null,
        int skip = 0,
        int take = 25
    );

    Task<RetrieveSingleResult<Role>> GetRoleAsync(Guid roleId, bool hydrate = false);

    Task<RetrieveListResult<User>> ListUsersAsync(
        FilterBuilder<User>? filter = null,
        OrderBuilder<User>? order = null,
        int skip = 0,
        int take = 25
    );

    Task<RetrieveSingleResult<User>> GetUserAsync(Guid userId, bool hydrate = false);

    Task<RetrieveListResult<Account>> ListAccountsAsync(
        FilterBuilder<Account>? filter = null,
        OrderBuilder<Account>? order = null,
        int skip = 0,
        int take = 25
    );

    Task<RetrieveSingleResult<Account>> GetAccountAsync(Guid accountId, bool hydrate = false);
}
