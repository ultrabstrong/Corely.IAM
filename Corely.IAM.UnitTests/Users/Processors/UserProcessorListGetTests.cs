using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Models;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Corely.Security.Encryption.Factories;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Users.Processors;

public class UserProcessorListGetTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly UserProcessor _userProcessor;
    private readonly Guid _accountId = Guid.CreateVersion7();

    public UserProcessorListGetTests()
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

        _userProcessor = new UserProcessor(
            _serviceFactory.GetRequiredService<IRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<IUserOwnershipProcessor>(),
            _serviceFactory.GetRequiredService<ISecurityProvider>(),
            _serviceFactory.GetRequiredService<ISymmetricEncryptionProviderFactory>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<ILogger<UserProcessor>>()
        );
    }

    private async Task<UserEntity> CreateUserEntityAsync(
        string username,
        Guid? accountId = null,
        List<AccountEntity>? accounts = null,
        List<GroupEntity>? groups = null,
        List<RoleEntity>? roles = null
    )
    {
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var effectiveAccountId = accountId ?? _accountId;
        var user = new UserEntity
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            Email = $"{username}@test.com",
            Accounts =
                accounts
                ?? [new AccountEntity { Id = effectiveAccountId, AccountName = "TestAccount" }],
            Groups = groups ?? [],
            Roles = roles ?? [],
        };
        return await userRepo.CreateAsync(user);
    }

    private async Task<AccountEntity> CreateAccountEntityAsync(string name, Guid? id = null)
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var account = new AccountEntity { Id = id ?? Guid.CreateVersion7(), AccountName = name };
        return await accountRepo.CreateAsync(account);
    }

    private async Task<GroupEntity> CreateGroupEntityAsync(string name)
    {
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            AccountId = _accountId,
            Users = [],
            Roles = [],
        };
        return await groupRepo.CreateAsync(group);
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
    public async Task ListUsersAsync_ReturnsPagedResults()
    {
        await CreateUserEntityAsync("user1");
        await CreateUserEntityAsync("user2");
        await CreateUserEntityAsync("user3");

        var result = await _userProcessor.ListUsersAsync(null, null, 0, 10);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Equal(3, result.Data.Items.Count);
    }

    [Fact]
    public async Task ListUsersAsync_ScopesToAccount()
    {
        var otherAccountId = Guid.CreateVersion7();
        await CreateUserEntityAsync("myuser");
        await CreateUserEntityAsync(
            "otheruser",
            accounts: [new AccountEntity { Id = otherAccountId, AccountName = "OtherAccount" }]
        );

        var result = await _userProcessor.ListUsersAsync(null, null, 0, 10);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal("myuser", result.Data.Items[0].Username);
    }

    [Fact]
    public async Task ListUsersAsync_AppliesPaging()
    {
        await CreateUserEntityAsync("user1");
        await CreateUserEntityAsync("user2");
        await CreateUserEntityAsync("user3");

        var result = await _userProcessor.ListUsersAsync(null, null, 0, 2);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Equal(2, result.Data.Items.Count);
        Assert.True(result.Data.HasMore);
    }

    [Fact]
    public async Task ListUsersAsync_ReturnsEmptyWhenNoUsers()
    {
        var result = await _userProcessor.ListUsersAsync(null, null, 0, 10);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.TotalCount);
        Assert.Empty(result.Data.Items);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsUserWhenFound()
    {
        var user = await CreateUserEntityAsync("testuser1");

        var result = await _userProcessor.GetUserByIdAsync(user.Id, hydrate: false);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(user.Id, result.Data.Id);
        Assert.Equal("testuser1", result.Data.Username);
        Assert.Null(result.Data.Accounts);
        Assert.Null(result.Data.Groups);
        Assert.Null(result.Data.Roles);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsNotFoundWhenUserDoesNotExist()
    {
        var result = await _userProcessor.GetUserByIdAsync(Guid.CreateVersion7(), hydrate: false);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetUserByIdAsync_HydratesAccountsGroupsAndRoles()
    {
        var group = await CreateGroupEntityAsync("TestGroup");
        var role = await CreateRoleEntityAsync("TestRole");
        var account = new AccountEntity { Id = _accountId, AccountName = "TestAccount" };
        var user = await CreateUserEntityAsync(
            "hydrateduser",
            accounts: [account],
            groups: [group],
            roles: [role]
        );

        var result = await _userProcessor.GetUserByIdAsync(user.Id, hydrate: true);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(user.Id, result.Data.Id);

        Assert.NotNull(result.Data.Accounts);
        Assert.Single(result.Data.Accounts);
        Assert.Equal(_accountId, result.Data.Accounts[0].Id);
        Assert.Equal("TestAccount", result.Data.Accounts[0].Name);

        Assert.NotNull(result.Data.Groups);
        Assert.Single(result.Data.Groups);
        Assert.Equal(group.Id, result.Data.Groups[0].Id);
        Assert.Equal("TestGroup", result.Data.Groups[0].Name);

        Assert.NotNull(result.Data.Roles);
        Assert.Single(result.Data.Roles);
        Assert.Equal(role.Id, result.Data.Roles[0].Id);
        Assert.Equal("TestRole", result.Data.Roles[0].Name);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsEmptyChildrenWhenHydratedWithNoChildren()
    {
        var user = await CreateUserEntityAsync("emptyuser");

        var result = await _userProcessor.GetUserByIdAsync(user.Id, hydrate: true);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Accounts);
        Assert.NotNull(result.Data.Groups);
        Assert.Empty(result.Data.Groups);
        Assert.NotNull(result.Data.Roles);
        Assert.Empty(result.Data.Roles);
    }
}
