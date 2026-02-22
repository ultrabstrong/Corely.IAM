using Corely.Common.Extensions;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.Services;

internal class RetrievalService(
    IPermissionProcessor permissionProcessor,
    IGroupProcessor groupProcessor,
    IRoleProcessor roleProcessor,
    IUserProcessor userProcessor,
    IAccountProcessor accountProcessor,
    IUserContextProvider userContextProvider
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
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public Task<RetrieveListResult<Permission>> ListPermissionsAsync(
        FilterBuilder<Permission>? filter = null,
        OrderBuilder<Permission>? order = null,
        int skip = 0,
        int take = 25
    ) => WrapListResultAsync(_permissionProcessor.ListPermissionsAsync(filter, order, skip, take));

    public async Task<RetrieveSingleResult<Permission>> GetPermissionAsync(
        Guid permissionId,
        bool hydrate = false
    )
    {
        var result = await _permissionProcessor.GetPermissionByIdAsync(permissionId, hydrate);
        var effectivePermissions = await GetEffectivePermissionsAsync(
            PermissionConstants.PERMISSION_RESOURCE_TYPE,
            permissionId
        );
        return new RetrieveSingleResult<Permission>(
            result.ResultCode,
            result.Message,
            result.Data,
            effectivePermissions
        );
    }

    public Task<RetrieveListResult<Group>> ListGroupsAsync(
        FilterBuilder<Group>? filter = null,
        OrderBuilder<Group>? order = null,
        int skip = 0,
        int take = 25
    ) => WrapListResultAsync(_groupProcessor.ListGroupsAsync(filter, order, skip, take));

    public async Task<RetrieveSingleResult<Group>> GetGroupAsync(Guid groupId, bool hydrate = false)
    {
        var result = await _groupProcessor.GetGroupByIdAsync(groupId, hydrate);
        var effectivePermissions = await GetEffectivePermissionsAsync(
            PermissionConstants.GROUP_RESOURCE_TYPE,
            groupId
        );
        return new RetrieveSingleResult<Group>(
            result.ResultCode,
            result.Message,
            result.Data,
            effectivePermissions
        );
    }

    public Task<RetrieveListResult<Role>> ListRolesAsync(
        FilterBuilder<Role>? filter = null,
        OrderBuilder<Role>? order = null,
        int skip = 0,
        int take = 25
    ) => WrapListResultAsync(_roleProcessor.ListRolesAsync(filter, order, skip, take));

    public async Task<RetrieveSingleResult<Role>> GetRoleAsync(Guid roleId, bool hydrate = false)
    {
        var result = await _roleProcessor.GetRoleByIdAsync(roleId, hydrate);
        var effectivePermissions = await GetEffectivePermissionsAsync(
            PermissionConstants.ROLE_RESOURCE_TYPE,
            roleId
        );
        return new RetrieveSingleResult<Role>(
            result.ResultCode,
            result.Message,
            result.Data,
            effectivePermissions
        );
    }

    public Task<RetrieveListResult<User>> ListUsersAsync(
        FilterBuilder<User>? filter = null,
        OrderBuilder<User>? order = null,
        int skip = 0,
        int take = 25
    ) => WrapListResultAsync(_userProcessor.ListUsersAsync(filter, order, skip, take));

    public async Task<RetrieveSingleResult<User>> GetUserAsync(Guid userId, bool hydrate = false)
    {
        var result = await _userProcessor.GetUserByIdAsync(userId, hydrate);
        var effectivePermissions = await GetEffectivePermissionsAsync(
            PermissionConstants.USER_RESOURCE_TYPE,
            userId
        );
        return new RetrieveSingleResult<User>(
            result.ResultCode,
            result.Message,
            result.Data,
            effectivePermissions
        );
    }

    public Task<RetrieveListResult<Account>> ListAccountsAsync(
        FilterBuilder<Account>? filter = null,
        OrderBuilder<Account>? order = null,
        int skip = 0,
        int take = 25
    ) => WrapListResultAsync(_accountProcessor.ListAccountsAsync(filter, order, skip, take));

    public async Task<RetrieveSingleResult<Account>> GetAccountAsync(
        Guid accountId,
        bool hydrate = false
    )
    {
        var result = await _accountProcessor.GetAccountByIdAsync(accountId, hydrate);
        var effectivePermissions = await GetEffectivePermissionsAsync(
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            accountId
        );
        return new RetrieveSingleResult<Account>(
            result.ResultCode,
            result.Message,
            result.Data,
            effectivePermissions
        );
    }

    private async Task<List<EffectivePermission>> GetEffectivePermissionsAsync(
        string resourceType,
        Guid resourceId
    )
    {
        var userContext = _userContextProvider.GetUserContext();
        if (userContext?.CurrentAccount == null)
            return [];

        return await _permissionProcessor.GetEffectivePermissionsForUserAsync(
            resourceType,
            resourceId,
            userContext.User.Id,
            userContext.CurrentAccount.Id
        );
    }

    private static async Task<RetrieveListResult<T>> WrapListResultAsync<T>(
        Task<ListResult<T>> resultTask
    )
    {
        var result = await resultTask;
        return new RetrieveListResult<T>(result.ResultCode, result.Message, result.Data);
    }
}
