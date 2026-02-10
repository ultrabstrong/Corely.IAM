using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Ordering;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
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
    IReadonlyRepo<PermissionEntity> permissionRepo,
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
    private readonly IReadonlyRepo<PermissionEntity> _permissionRepo = permissionRepo.ThrowIfNull(
        nameof(permissionRepo)
    );
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
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
        var userContext = _userContextProvider.GetUserContext()!;
        var accountId = userContext.CurrentAccount!.Id;
        var userId = userContext.User.Id;

        var effectivePermissions = await _permissionRepo.QueryAsync(q =>
            q.Where(p =>
                    p.AccountId == accountId
                    && (
                        p.ResourceType == resourceType
                        || p.ResourceType == PermissionConstants.ALL_RESOURCE_TYPES
                    )
                    && (p.ResourceId == resourceId || p.ResourceId == Guid.Empty)
                    && p.Roles!.Any(r =>
                        r.Users!.Any(u => u.Id == userId)
                        || r.Groups!.Any(g => g.Users!.Any(u => u.Id == userId))
                    )
                )
                .Select(p => new EffectivePermission
                {
                    PermissionId = p.Id,
                    Create = p.Create,
                    Read = p.Read,
                    Update = p.Update,
                    Delete = p.Delete,
                    Execute = p.Execute,
                    Description = p.Description,
                    ResourceType = p.ResourceType,
                    ResourceId = p.ResourceId,
                    Roles = p.Roles!.Where(r =>
                            r.Users!.Any(u => u.Id == userId)
                            || r.Groups!.Any(g => g.Users!.Any(u => u.Id == userId))
                        )
                        .Select(r => new EffectiveRole
                        {
                            RoleId = r.Id,
                            RoleName = r.Name,
                            IsDirect = r.Users!.Any(u => u.Id == userId),
                            Groups = r.Groups!.Where(g => g.Users!.Any(u => u.Id == userId))
                                .Select(g => new EffectiveGroup
                                {
                                    GroupId = g.Id,
                                    GroupName = g.Name,
                                })
                                .ToList(),
                        })
                        .ToList(),
                })
        );

        return effectivePermissions.ToList();
    }
}
