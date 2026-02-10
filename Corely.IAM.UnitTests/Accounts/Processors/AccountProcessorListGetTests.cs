using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Accounts.Processors;

public class AccountProcessorListGetTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly AccountProcessor _accountProcessor;
    private readonly Guid _accountId = Guid.CreateVersion7();

    public AccountProcessorListGetTests()
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

        _accountProcessor = new AccountProcessor(
            _serviceFactory.GetRequiredService<IRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IUserOwnershipProcessor>(),
            _serviceFactory.GetRequiredService<ISecurityProvider>(),
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<AccountProcessor>>()
        );
    }

    private async Task<AccountEntity> CreateAccountEntityAsync(
        string name,
        Guid? accountId = null,
        List<UserEntity>? users = null,
        List<GroupEntity>? groups = null,
        List<RoleEntity>? roles = null,
        List<PermissionEntity>? permissions = null
    )
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var id = accountId ?? _accountId;
        var account = new AccountEntity
        {
            Id = id,
            AccountName = name,
            Users = users ?? [],
            Groups = groups ?? [],
            Roles = roles ?? [],
            Permissions = permissions ?? [],
        };
        return await accountRepo.CreateAsync(account);
    }

    private async Task<UserEntity> CreateUserEntityAsync(string username)
    {
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = new UserEntity
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            Accounts = [],
            Groups = [],
            Roles = [],
        };
        return await userRepo.CreateAsync(user);
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

    private async Task<PermissionEntity> CreatePermissionEntityAsync(
        string resourceType,
        string? description = null
    )
    {
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permission = new PermissionEntity
        {
            Id = Guid.CreateVersion7(),
            ResourceType = resourceType,
            Description = description,
            AccountId = _accountId,
            Roles = [],
        };
        return await permissionRepo.CreateAsync(permission);
    }

    [Fact]
    public async Task ListAccountsAsync_ReturnsPagedResults()
    {
        await CreateAccountEntityAsync("TestAccount");

        var result = await _accountProcessor.ListAccountsAsync(null, null, 0, 10);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCount);
        Assert.Single(result.Data.Items);
    }

    [Fact]
    public async Task ListAccountsAsync_ScopesToUserAccounts()
    {
        var otherAccountId = Guid.CreateVersion7();
        await CreateAccountEntityAsync("MyAccount");
        await CreateAccountEntityAsync("OtherAccount", otherAccountId);

        var result = await _accountProcessor.ListAccountsAsync(null, null, 0, 10);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal("MyAccount", result.Data.Items[0].AccountName);
    }

    [Fact]
    public async Task ListAccountsAsync_AppliesPaging()
    {
        // Set up user context with multiple accounts
        var accountId2 = Guid.CreateVersion7();
        var accountId3 = Guid.CreateVersion7();
        var userContextSetter = _serviceFactory.GetRequiredService<IUserContextSetter>();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = "testuser",
            Email = "test@test.com",
        };
        var account1 = new Account { Id = _accountId, AccountName = "Account1" };
        var account2 = new Account { Id = accountId2, AccountName = "Account2" };
        var account3 = new Account { Id = accountId3, AccountName = "Account3" };
        userContextSetter.SetUserContext(
            new UserContext(user, account1, "device1", [account1, account2, account3])
        );

        await CreateAccountEntityAsync("Account1");
        await CreateAccountEntityAsync("Account2", accountId2);
        await CreateAccountEntityAsync("Account3", accountId3);

        var result = await _accountProcessor.ListAccountsAsync(null, null, 0, 2);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Equal(2, result.Data.Items.Count);
        Assert.True(result.Data.HasMore);
    }

    [Fact]
    public async Task ListAccountsAsync_ReturnsEmptyWhenNoAccounts()
    {
        // User context has _accountId but no entity created for it
        // Need a fresh service factory with no accounts
        var serviceFactory = new ServiceFactory();
        var emptyAccountId = Guid.CreateVersion7();
        var userContextSetter = serviceFactory.GetRequiredService<IUserContextSetter>();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = "testuser",
            Email = "test@test.com",
        };
        var account = new Account { Id = emptyAccountId, AccountName = "Empty" };
        userContextSetter.SetUserContext(new UserContext(user, account, "device1", [account]));

        var processor = new AccountProcessor(
            serviceFactory.GetRequiredService<IRepo<AccountEntity>>(),
            serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            serviceFactory.GetRequiredService<IUserOwnershipProcessor>(),
            serviceFactory.GetRequiredService<ISecurityProvider>(),
            serviceFactory.GetRequiredService<IUserContextProvider>(),
            serviceFactory.GetRequiredService<IValidationProvider>(),
            serviceFactory.GetRequiredService<ILogger<AccountProcessor>>()
        );

        var result = await processor.ListAccountsAsync(null, null, 0, 10);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.TotalCount);
        Assert.Empty(result.Data.Items);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ReturnsAccountWhenFound()
    {
        await CreateAccountEntityAsync("TestAccount");

        var result = await _accountProcessor.GetAccountByIdAsync(_accountId, hydrate: false);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(_accountId, result.Data.Id);
        Assert.Equal("TestAccount", result.Data.AccountName);
        Assert.Null(result.Data.Users);
        Assert.Null(result.Data.Groups);
        Assert.Null(result.Data.Roles);
        Assert.Null(result.Data.Permissions);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ReturnsNotFoundWhenAccountDoesNotExist()
    {
        var result = await _accountProcessor.GetAccountByIdAsync(
            Guid.CreateVersion7(),
            hydrate: false
        );

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ReturnsNotFoundWhenUserLacksAccess()
    {
        var otherAccountId = Guid.CreateVersion7();
        await CreateAccountEntityAsync("OtherAccount", otherAccountId);

        var result = await _accountProcessor.GetAccountByIdAsync(otherAccountId, hydrate: false);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetAccountByIdAsync_HydratesChildren()
    {
        var user = await CreateUserEntityAsync("testuser1");
        var group = await CreateGroupEntityAsync("TestGroup");
        var role = await CreateRoleEntityAsync("TestRole");
        var permission = await CreatePermissionEntityAsync("test_resource", "Test Permission");

        await CreateAccountEntityAsync(
            "HydratedAccount",
            users: [user],
            groups: [group],
            roles: [role],
            permissions: [permission]
        );

        var result = await _accountProcessor.GetAccountByIdAsync(_accountId, hydrate: true);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(_accountId, result.Data.Id);

        Assert.NotNull(result.Data.Users);
        Assert.Single(result.Data.Users);
        Assert.Equal(user.Id, result.Data.Users[0].Id);
        Assert.Equal("testuser1", result.Data.Users[0].Name);

        Assert.NotNull(result.Data.Groups);
        Assert.Single(result.Data.Groups);
        Assert.Equal(group.Id, result.Data.Groups[0].Id);
        Assert.Equal("TestGroup", result.Data.Groups[0].Name);

        Assert.NotNull(result.Data.Roles);
        Assert.Single(result.Data.Roles);
        Assert.Equal(role.Id, result.Data.Roles[0].Id);
        Assert.Equal("TestRole", result.Data.Roles[0].Name);

        Assert.NotNull(result.Data.Permissions);
        Assert.Single(result.Data.Permissions);
        Assert.Equal(permission.Id, result.Data.Permissions[0].Id);
        Assert.Equal("Test Permission", result.Data.Permissions[0].Name);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ReturnsEmptyChildrenWhenHydratedWithNoChildren()
    {
        await CreateAccountEntityAsync("EmptyAccount");

        var result = await _accountProcessor.GetAccountByIdAsync(_accountId, hydrate: true);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Users);
        Assert.Empty(result.Data.Users);
        Assert.NotNull(result.Data.Groups);
        Assert.Empty(result.Data.Groups);
        Assert.NotNull(result.Data.Roles);
        Assert.Empty(result.Data.Roles);
        Assert.NotNull(result.Data.Permissions);
        Assert.Empty(result.Data.Permissions);
    }
}
