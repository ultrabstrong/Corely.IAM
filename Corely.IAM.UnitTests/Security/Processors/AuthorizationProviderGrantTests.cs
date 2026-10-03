using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Security.Processors;

public class AuthorizationProviderGrantTests
{
    private const string INVOICE = "invoice";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly Guid _otherAccountId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private RoleEntity _callerRole = null!;

    [Fact]
    public async Task CanGrantAsync_ReturnsTrue_ForSystemContext()
    {
        SetContext(new UserContext(true, "system"));

        Assert.True(await CreateProvider().CanGrantAsync(INVOICE, Guid.Empty, AuthAction.Create));
    }

    [Fact]
    public async Task CanGrantAsync_ReturnsFalse_ForNoUserContext()
    {
        Assert.False(await CreateProvider().CanGrantAsync(INVOICE, Guid.Empty, AuthAction.Read));
    }

    [Fact]
    public async Task CanGrantAsync_ReturnsTrue_ForGrantFullyCovered()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Read, AuthAction.Update);

        Assert.True(
            await CreateProvider()
                .CanGrantAsync(INVOICE, Guid.Empty, AuthAction.Read, AuthAction.Update)
        );
    }

    [Fact]
    public async Task CanGrantAsync_ReturnsTrue_ForActionsSplitAcrossHeldRows()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Read);
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Update);

        Assert.True(
            await CreateProvider()
                .CanGrantAsync(INVOICE, Guid.Empty, AuthAction.Read, AuthAction.Update)
        );
    }

    [Fact]
    public async Task CanGrantAsync_ReturnsFalse_ForGrantPartlyCovered()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Read);

        Assert.False(
            await CreateProvider()
                .CanGrantAsync(INVOICE, Guid.Empty, AuthAction.Read, AuthAction.Create)
        );
    }

    [Fact]
    public async Task CanGrantAsync_ReturnsTrue_ForSpecificIdUnderHeldAllIds()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Read);

        Assert.True(
            await CreateProvider().CanGrantAsync(INVOICE, Guid.CreateVersion7(), AuthAction.Read)
        );
    }

    [Fact]
    public async Task CanGrantAsync_ReturnsFalse_ForAllIdsUnderHeldSpecificId()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.CreateVersion7(), AuthAction.Read);

        Assert.False(await CreateProvider().CanGrantAsync(INVOICE, Guid.Empty, AuthAction.Read));
    }

    [Fact]
    public async Task CanGrantAsync_ReturnsFalse_ForAnotherResourceType()
    {
        await SignInCallerAsync();
        await HoldAsync("report", Guid.Empty, AuthAction.Read);

        Assert.False(await CreateProvider().CanGrantAsync(INVOICE, Guid.Empty, AuthAction.Read));
    }

    [Fact]
    public async Task CanGrantPermissionsAsync_ReturnsTrue_ForCoveredPermissions()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Read, AuthAction.Update);
        var grant = await CreatePermissionAsync(_accountId, INVOICE, AuthAction.Read);

        Assert.True(await CreateProvider().CanGrantPermissionsAsync([grant.Id]));
    }

    [Fact]
    public async Task CanGrantPermissionsAsync_ReturnsFalse_ForAnyUncoveredPermission()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Read);
        var covered = await CreatePermissionAsync(_accountId, INVOICE, AuthAction.Read);
        var uncovered = await CreatePermissionAsync(_accountId, INVOICE, AuthAction.Create);

        Assert.False(await CreateProvider().CanGrantPermissionsAsync([covered.Id, uncovered.Id]));
    }

    [Fact]
    public async Task CanGrantPermissionsAsync_HandsOutNothing_ForPermissionFromAnotherAccount()
    {
        await SignInCallerAsync();
        var foreign = await CreatePermissionAsync(_otherAccountId, INVOICE, AuthAction.Create);

        Assert.True(await CreateProvider().CanGrantPermissionsAsync([foreign.Id]));
    }

    [Fact]
    public async Task CanGrantRolesAsync_ReturnsFalse_ForRoleHoldingMoreThanTheCaller()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Read);
        var role = await CreateRoleWithAsync(
            await CreatePermissionAsync(_accountId, INVOICE, AuthAction.Read, AuthAction.Delete)
        );

        Assert.False(await CreateProvider().CanGrantRolesAsync([role.Id]));
    }

    [Fact]
    public async Task CanGrantRolesAsync_ReturnsTrue_ForRoleWithinTheCaller()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Read, AuthAction.Delete);
        var role = await CreateRoleWithAsync(
            await CreatePermissionAsync(_accountId, INVOICE, AuthAction.Delete)
        );

        Assert.True(await CreateProvider().CanGrantRolesAsync([role.Id]));
    }

    [Fact]
    public async Task CanGrantGroupAsync_ReturnsFalse_ForGroupWhoseRolesHoldMoreThanTheCaller()
    {
        await SignInCallerAsync();
        var role = await CreateRoleWithAsync(
            await CreatePermissionAsync(_accountId, INVOICE, AuthAction.Execute)
        );
        var group = await CreateGroupWithAsync(role);

        Assert.False(await CreateProvider().CanGrantGroupAsync(group.Id));
    }

    [Fact]
    public async Task CanGrantGroupAsync_ReturnsTrue_ForGroupWhoseRolesAreWithinTheCaller()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Execute);
        var role = await CreateRoleWithAsync(
            await CreatePermissionAsync(_accountId, INVOICE, AuthAction.Execute)
        );
        var group = await CreateGroupWithAsync(role);

        Assert.True(await CreateProvider().CanGrantGroupAsync(group.Id));
    }

    private AuthorizationProvider CreateProvider() =>
        new(
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<PermissionEntity>>(),
            _serviceFactory.GetRequiredService<ILogger<AuthorizationProvider>>(),
            Options.Create(new SecurityOptions { PermissionCacheTtlSeconds = 30 }),
            TimeProvider.System
        );

    private void SetContext(UserContext context) =>
        (
            (IUserContextSetter)_serviceFactory.GetRequiredService<UserContextProvider>()
        ).SetUserContext(context);

    [Fact]
    public async Task GetGrantableActionsAsync_ReturnsHeldActions_ForType()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Read, AuthAction.Update);

        var actions = await CreateProvider().GetGrantableActionsAsync(INVOICE, Guid.Empty);

        Assert.Equal([AuthAction.Read, AuthAction.Update], actions.Order());
    }

    [Fact]
    public async Task GetGrantableActionsAsync_ReturnsEveryAction_ForSystemContext()
    {
        SetContext(new UserContext(true, "system"));

        var actions = await CreateProvider().GetGrantableActionsAsync(INVOICE, Guid.Empty);

        Assert.Equal(Enum.GetValues<AuthAction>().Length, actions.Count);
    }

    [Fact]
    public async Task GetGrantableActionsAsync_ReturnsNothing_ForNoUserContext()
    {
        Assert.Empty(await CreateProvider().GetGrantableActionsAsync(INVOICE, Guid.Empty));
    }

    [Fact]
    public async Task GetGrantablePermissionIdsAsync_ReturnsOnlyCovered_ForMixedPermissions()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Read);
        var covered = await CreatePermissionAsync(_accountId, INVOICE, AuthAction.Read);
        var uncovered = await CreatePermissionAsync(_accountId, INVOICE, AuthAction.Create);

        var grantable = await CreateProvider()
            .GetGrantablePermissionIdsAsync([covered.Id, uncovered.Id]);

        Assert.Equal([covered.Id], grantable);
    }

    [Fact]
    public async Task GetGrantableRoleIdsAsync_ReturnsOnlyRolesWithinTheCaller_ForMixedRoles()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Read);
        var within = await CreateRoleWithAsync(
            await CreatePermissionAsync(_accountId, INVOICE, AuthAction.Read)
        );
        var beyond = await CreateRoleWithAsync(
            await CreatePermissionAsync(_accountId, INVOICE, AuthAction.Delete)
        );

        var grantable = await CreateProvider().GetGrantableRoleIdsAsync([within.Id, beyond.Id]);

        Assert.Equal([within.Id], grantable);
    }

    [Fact]
    public async Task GetGrantableGroupIdsAsync_ReturnsOnlyGroupsWithinTheCaller_ForMixedGroups()
    {
        await SignInCallerAsync();
        await HoldAsync(INVOICE, Guid.Empty, AuthAction.Read);
        var within = await CreateGroupWithAsync(
            await CreateRoleWithAsync(
                await CreatePermissionAsync(_accountId, INVOICE, AuthAction.Read)
            )
        );
        var beyond = await CreateGroupWithAsync(
            await CreateRoleWithAsync(
                await CreatePermissionAsync(_accountId, INVOICE, AuthAction.Execute)
            )
        );

        var grantable = await CreateProvider().GetGrantableGroupIdsAsync([within.Id, beyond.Id]);

        Assert.Equal([within.Id], grantable);
    }

    private async Task SignInCallerAsync()
    {
        await _serviceFactory
            .GetRequiredService<IRepo<AccountEntity>>()
            .CreateAsync(new AccountEntity { Id = _accountId, AccountName = "Caller account" });
        var user = await _serviceFactory
            .GetRequiredService<IRepo<UserEntity>>()
            .CreateAsync(
                new UserEntity
                {
                    Id = _userId,
                    Username = "caller",
                    Email = "caller@example.com",
                }
            );
        _callerRole = await _serviceFactory
            .GetRequiredService<IRepo<RoleEntity>>()
            .CreateAsync(
                new RoleEntity
                {
                    Id = Guid.CreateVersion7(),
                    Name = "Caller",
                    AccountId = _accountId,
                    Users = [user],
                    Groups = [],
                }
            );

        var account = new Account { Id = _accountId };
        SetContext(new UserContext(new User { Id = _userId }, account, "device", [account]));
    }

    private async Task HoldAsync(string resourceType, Guid resourceId, params AuthAction[] actions)
    {
        var permission = NewPermission(_accountId, resourceType, resourceId, actions);
        permission.Roles = [_callerRole];
        await _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>().CreateAsync(permission);
    }

    private Task<PermissionEntity> CreatePermissionAsync(
        Guid accountId,
        string resourceType,
        params AuthAction[] actions
    ) =>
        _serviceFactory
            .GetRequiredService<IRepo<PermissionEntity>>()
            .CreateAsync(NewPermission(accountId, resourceType, Guid.Empty, actions));

    private async Task<RoleEntity> CreateRoleWithAsync(PermissionEntity permission)
    {
        var role = await _serviceFactory
            .GetRequiredService<IRepo<RoleEntity>>()
            .CreateAsync(
                new RoleEntity
                {
                    Id = Guid.CreateVersion7(),
                    Name = "Target",
                    AccountId = _accountId,
                    Users = [],
                    Groups = [],
                }
            );
        permission.Roles = [role];
        return role;
    }

    private async Task<GroupEntity> CreateGroupWithAsync(RoleEntity role)
    {
        var group = await _serviceFactory
            .GetRequiredService<IRepo<GroupEntity>>()
            .CreateAsync(
                new GroupEntity
                {
                    Id = Guid.CreateVersion7(),
                    Name = "Target group",
                    AccountId = _accountId,
                    Roles = [role],
                    Users = [],
                }
            );
        role.Groups = [group];
        return group;
    }

    private static PermissionEntity NewPermission(
        Guid accountId,
        string resourceType,
        Guid resourceId,
        AuthAction[] actions
    ) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Create = actions.Contains(AuthAction.Create),
            Read = actions.Contains(AuthAction.Read),
            Update = actions.Contains(AuthAction.Update),
            Delete = actions.Contains(AuthAction.Delete),
            Execute = actions.Contains(AuthAction.Execute),
            Roles = [],
        };
}
