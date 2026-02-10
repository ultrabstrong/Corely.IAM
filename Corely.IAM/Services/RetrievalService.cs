using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Ordering;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;

namespace Corely.IAM.Services;

internal class RetrievalService(
    IPermissionProcessor permissionProcessor,
    IGroupProcessor groupProcessor,
    IRoleProcessor roleProcessor,
    IUserProcessor userProcessor,
    IAccountProcessor accountProcessor
) : IRetrievalService
{
    private readonly IPermissionProcessor _permissionProcessor = permissionProcessor.ThrowIfNull(
        nameof(permissionProcessor)
    );
    private readonly IGroupProcessor _groupProcessor = groupProcessor.ThrowIfNull(
        nameof(groupProcessor)
    );
    private readonly IRoleProcessor _roleProcessor = roleProcessor.ThrowIfNull(
        nameof(roleProcessor)
    );
    private readonly IUserProcessor _userProcessor = userProcessor.ThrowIfNull(
        nameof(userProcessor)
    );
    private readonly IAccountProcessor _accountProcessor = accountProcessor.ThrowIfNull(
        nameof(accountProcessor)
    );

    public async Task<RetrieveListResult<Permission>> ListPermissionsAsync(
        FilterBuilder<Permission>? filter = null,
        OrderBuilder<Permission>? order = null,
        int skip = 0,
        int take = 25
    )
    {
        var result = await _permissionProcessor.ListPermissionsAsync(filter, order, skip, take);
        return new RetrieveListResult<Permission>(result.ResultCode, result.Message, result.Data);
    }

    public async Task<RetrieveSingleResult<Permission>> GetPermissionAsync(
        Guid permissionId,
        bool hydrate = false
    )
    {
        var result = await _permissionProcessor.GetPermissionByIdAsync(permissionId, hydrate);
        return new RetrieveSingleResult<Permission>(
            result.ResultCode,
            result.Message,
            result.Data,
            null
        );
    }

    public async Task<RetrieveListResult<Group>> ListGroupsAsync(
        FilterBuilder<Group>? filter = null,
        OrderBuilder<Group>? order = null,
        int skip = 0,
        int take = 25
    )
    {
        var result = await _groupProcessor.ListGroupsAsync(filter, order, skip, take);
        return new RetrieveListResult<Group>(result.ResultCode, result.Message, result.Data);
    }

    public async Task<RetrieveSingleResult<Group>> GetGroupAsync(Guid groupId, bool hydrate = false)
    {
        var result = await _groupProcessor.GetGroupByIdAsync(groupId, hydrate);
        return new RetrieveSingleResult<Group>(
            result.ResultCode,
            result.Message,
            result.Data,
            null
        );
    }

    public async Task<RetrieveListResult<Role>> ListRolesAsync(
        FilterBuilder<Role>? filter = null,
        OrderBuilder<Role>? order = null,
        int skip = 0,
        int take = 25
    )
    {
        var result = await _roleProcessor.ListRolesAsync(filter, order, skip, take);
        return new RetrieveListResult<Role>(result.ResultCode, result.Message, result.Data);
    }

    public async Task<RetrieveSingleResult<Role>> GetRoleAsync(Guid roleId, bool hydrate = false)
    {
        var result = await _roleProcessor.GetRoleByIdAsync(roleId, hydrate);
        return new RetrieveSingleResult<Role>(result.ResultCode, result.Message, result.Data, null);
    }

    public async Task<RetrieveListResult<User>> ListUsersAsync(
        FilterBuilder<User>? filter = null,
        OrderBuilder<User>? order = null,
        int skip = 0,
        int take = 25
    )
    {
        var result = await _userProcessor.ListUsersAsync(filter, order, skip, take);
        return new RetrieveListResult<User>(result.ResultCode, result.Message, result.Data);
    }

    public async Task<RetrieveSingleResult<User>> GetUserAsync(Guid userId, bool hydrate = false)
    {
        var result = await _userProcessor.GetUserByIdAsync(userId, hydrate);
        return new RetrieveSingleResult<User>(result.ResultCode, result.Message, result.Data, null);
    }

    public async Task<RetrieveListResult<Account>> ListAccountsAsync(
        FilterBuilder<Account>? filter = null,
        OrderBuilder<Account>? order = null,
        int skip = 0,
        int take = 25
    )
    {
        var result = await _accountProcessor.ListAccountsAsync(filter, order, skip, take);
        return new RetrieveListResult<Account>(result.ResultCode, result.Message, result.Data);
    }

    public async Task<RetrieveSingleResult<Account>> GetAccountAsync(
        Guid accountId,
        bool hydrate = false
    )
    {
        var result = await _accountProcessor.GetAccountByIdAsync(accountId, hydrate);
        return new RetrieveSingleResult<Account>(
            result.ResultCode,
            result.Message,
            result.Data,
            null
        );
    }
}
