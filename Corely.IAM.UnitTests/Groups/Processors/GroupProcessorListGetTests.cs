using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Groups.Processors;

public class GroupProcessorListGetTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly GroupProcessor _groupProcessor;
    private readonly Guid _accountId = Guid.CreateVersion7();

    public GroupProcessorListGetTests()
    {
        var userContextSetter = _serviceFactory.GetRequiredService<IUserContextSetter>();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = "testuser",
            Email = "test@test.com",
        };
        var account = new Account { Id = _accountId, AccountName = "TestAccount" };
        userContextSetter.SetUserContext(new UserContext(user, account, "device1", [account]));

        _groupProcessor = new GroupProcessor(
            _serviceFactory.GetRequiredService<IRepo<GroupEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<IUserOwnershipProcessor>(),
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<GroupProcessor>>()
        );
    }

    private async Task<GroupEntity> CreateGroupEntityAsync(
        string name,
        Guid? accountId = null,
        List<UserEntity>? users = null,
        List<RoleEntity>? roles = null
    )
    {
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            AccountId = accountId ?? _accountId,
            Users = users ?? [],
            Roles = roles ?? [],
        };
        return await groupRepo.CreateAsync(group);
    }

    private async Task<UserEntity> CreateUserEntityAsync(string username)
    {
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = new UserEntity
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            Accounts = [new AccountEntity { Id = _accountId }],
            Groups = [],
            Roles = [],
        };
        return await userRepo.CreateAsync(user);
    }

    private async Task<RoleEntity> CreateRoleEntityAsync(string name)
    {
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var role = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            AccountId = _accountId,
            Account = new AccountEntity { Id = _accountId },
            Groups = [],
            Users = [],
            Permissions = [],
        };
        return await roleRepo.CreateAsync(role);
    }

    [Fact]
    public async Task ListGroupsAsync_ReturnsPagedResults()
    {
        await CreateGroupEntityAsync("Group1");
        await CreateGroupEntityAsync("Group2");
        await CreateGroupEntityAsync("Group3");

        var result = await _groupProcessor.ListGroupsAsync(null, null, 0, 10);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Equal(3, result.Data.Items.Count);
    }

    [Fact]
    public async Task ListGroupsAsync_ScopesToAccount()
    {
        var otherAccountId = Guid.CreateVersion7();
        await CreateGroupEntityAsync("MyGroup");
        await CreateGroupEntityAsync("OtherGroup", otherAccountId);

        var result = await _groupProcessor.ListGroupsAsync(null, null, 0, 10);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal("MyGroup", result.Data.Items[0].Name);
    }

    [Fact]
    public async Task ListGroupsAsync_AppliesPaging()
    {
        await CreateGroupEntityAsync("Group1");
        await CreateGroupEntityAsync("Group2");
        await CreateGroupEntityAsync("Group3");

        var result = await _groupProcessor.ListGroupsAsync(null, null, 0, 2);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Equal(2, result.Data.Items.Count);
        Assert.True(result.Data.HasMore);
    }

    [Fact]
    public async Task ListGroupsAsync_ReturnsEmptyWhenNoGroups()
    {
        var result = await _groupProcessor.ListGroupsAsync(null, null, 0, 10);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.TotalCount);
        Assert.Empty(result.Data.Items);
    }

    [Fact]
    public async Task GetGroupByIdAsync_ReturnsGroupWhenFound()
    {
        var group = await CreateGroupEntityAsync("TestGroup");

        var result = await _groupProcessor.GetGroupByIdAsync(group.Id, hydrate: false);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(group.Id, result.Data.Id);
        Assert.Equal("TestGroup", result.Data.Name);
        Assert.Null(result.Data.Users);
        Assert.Null(result.Data.Roles);
    }

    [Fact]
    public async Task GetGroupByIdAsync_ReturnsNotFoundWhenGroupDoesNotExist()
    {
        var result = await _groupProcessor.GetGroupByIdAsync(Guid.CreateVersion7(), hydrate: false);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetGroupByIdAsync_HydratesUsersAndRoles()
    {
        var user = await CreateUserEntityAsync("testuser1");
        var role = await CreateRoleEntityAsync("TestRole");
        var group = await CreateGroupEntityAsync("HydratedGroup", users: [user], roles: [role]);

        var result = await _groupProcessor.GetGroupByIdAsync(group.Id, hydrate: true);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(group.Id, result.Data.Id);

        Assert.NotNull(result.Data.Users);
        Assert.Single(result.Data.Users);
        Assert.Equal(user.Id, result.Data.Users[0].Id);
        Assert.Equal("testuser1", result.Data.Users[0].Name);

        Assert.NotNull(result.Data.Roles);
        Assert.Single(result.Data.Roles);
        Assert.Equal(role.Id, result.Data.Roles[0].Id);
        Assert.Equal("TestRole", result.Data.Roles[0].Name);
    }

    [Fact]
    public async Task GetGroupByIdAsync_ReturnsEmptyChildrenWhenHydratedWithNoChildren()
    {
        var group = await CreateGroupEntityAsync("EmptyGroup");

        var result = await _groupProcessor.GetGroupByIdAsync(group.Id, hydrate: true);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Users);
        Assert.Empty(result.Data.Users);
        Assert.NotNull(result.Data.Roles);
        Assert.Empty(result.Data.Roles);
    }
}
