using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Groups.Processors;

/// <summary>
/// A list must return only what the caller is permitted to see. Authorizing the call as a whole
/// and then returning every row leaks resources the caller cannot open, and makes paging wrong -
/// a page of 50 filtered afterwards is no longer a page of 50.
/// </summary>
public class GroupListAuthorizationScopeTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly GroupProcessorAuthorizationDecorator _decorator;
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();

    public GroupListAuthorizationScopeTests()
    {
        var userContextSetter = _serviceFactory.GetRequiredService<IUserContextSetter>();
        var user = new User
        {
            Id = _userId,
            Username = "testuser",
            Email = "test@test.com",
        };
        var account = new Account { Id = _accountId, AccountName = "TestAccount" };
        userContextSetter.SetUserContext(new UserContext(user, account, "device1", [account]));

        var processor = new GroupProcessor(
            _serviceFactory.GetRequiredService<IRepo<GroupEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<IUserOwnershipProcessor>(),
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<GroupProcessor>>()
        );

        var authorizationProvider = new AuthorizationProvider(
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<PermissionEntity>>(),
            _serviceFactory.GetRequiredService<ILogger<AuthorizationProvider>>(),
            Options.Create(new SecurityOptions()),
            TimeProvider.System
        );

        _decorator = new GroupProcessorAuthorizationDecorator(processor, authorizationProvider);
    }

    [Fact]
    public async Task ListGroups_ReturnsOnlyPermittedGroups_WhenPermissionIsPerResource()
    {
        var permitted = await CreateGroupAsync("permitted");
        await CreateGroupAsync("forbidden");
        await GrantReadAsync(permitted.Id);

        var result = await _decorator.ListGroupsAsync(new(_accountId, Take: 10));

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.Single(result.Data!.Items);
        Assert.Equal(permitted.Id, result.Data.Items[0].Id);
    }

    [Fact]
    public async Task ListGroups_TotalCountReflectsOnlyPermittedGroups()
    {
        var permitted = await CreateGroupAsync("permitted");
        await CreateGroupAsync("forbidden one");
        await CreateGroupAsync("forbidden two");
        await GrantReadAsync(permitted.Id);

        var result = await _decorator.ListGroupsAsync(new(_accountId, Take: 10));

        // Paging is computed from the total, so a total that counts rows the caller cannot see
        // reports pages that do not exist.
        Assert.Equal(1, result.Data!.TotalCount);
    }

    [Fact]
    public async Task ListGroups_ReturnsEverything_WhenPermissionIsWildcard()
    {
        await CreateGroupAsync("one");
        await CreateGroupAsync("two");
        await GrantReadAsync(Guid.Empty);

        var result = await _decorator.ListGroupsAsync(new(_accountId, Take: 10));

        Assert.Equal(2, result.Data!.Items.Count);
    }

    [Fact]
    public async Task ListGroups_ReturnsUnauthorized_WhenNoReadPermissionAtAll()
    {
        await CreateGroupAsync("one");

        var result = await _decorator.ListGroupsAsync(new(_accountId, Take: 10));

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
    }

    private async Task<GroupEntity> CreateGroupAsync(string name)
    {
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            AccountId = _accountId,
        };
        await groupRepo.CreateAsync(group);
        return group;
    }

    private async Task GrantReadAsync(Guid resourceId)
    {
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();

        var user = new UserEntity
        {
            Id = _userId,
            Username = "testuser",
            Email = "test@test.com",
        };
        await userRepo.CreateAsync(user);

        var role = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            Name = "TestRole",
            AccountId = _accountId,
            Users = [user],
            Groups = [],
        };
        await roleRepo.CreateAsync(role);

        await permissionRepo.CreateAsync(
            new PermissionEntity
            {
                Id = Guid.CreateVersion7(),
                AccountId = _accountId,
                ResourceType = PermissionConstants.GROUP_RESOURCE_TYPE,
                ResourceId = resourceId,
                Read = true,
                Roles = [role],
            }
        );
    }
}
