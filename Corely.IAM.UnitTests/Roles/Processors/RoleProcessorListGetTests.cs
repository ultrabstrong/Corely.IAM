using System.Linq.Expressions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Roles.Processors;

public class RoleProcessorListGetTests
{
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly ServiceFactory _serviceFactory = new();
    private readonly Mock<IUserContextProvider> _mockUserContextProvider = new();
    private readonly RoleProcessor _roleProcessor;

    public RoleProcessorListGetTests()
    {
        var userContext = new UserContext(
            new User
            {
                Id = Guid.CreateVersion7(),
                Username = "testuser",
                Email = "test@test.com",
            },
            new Account { Id = _accountId, AccountName = "TestAccount" },
            "device1",
            []
        );
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(userContext);

        _roleProcessor = new RoleProcessor(
            _serviceFactory.GetRequiredService<IRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<PermissionEntity>>(),
            _mockUserContextProvider.Object,
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<RoleProcessor>>()
        );
    }

    private async Task<(RoleEntity Role, AccountEntity Account)> CreateRoleAsync(
        string roleName = "TestRole"
    )
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var account = await accountRepo.CreateAsync(
            new AccountEntity { Id = _accountId, AccountName = "TestAccount" }
        );

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var role = await roleRepo.CreateAsync(
            new RoleEntity
            {
                Id = Guid.CreateVersion7(),
                Name = roleName,
                AccountId = account.Id,
                Account = account,
            }
        );
        return (role, account);
    }

    private async Task<RoleEntity> CreateRoleWithChildrenAsync()
    {
        var (role, account) = await CreateRoleAsync();

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user1 = await userRepo.CreateAsync(
            new UserEntity
            {
                Id = Guid.CreateVersion7(),
                Username = "user1",
                Email = "u1@test.com",
            }
        );
        var user2 = await userRepo.CreateAsync(
            new UserEntity
            {
                Id = Guid.CreateVersion7(),
                Username = "user2",
                Email = "u2@test.com",
            }
        );

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group1 = await groupRepo.CreateAsync(
            new GroupEntity
            {
                Id = Guid.CreateVersion7(),
                Name = "Group1",
                AccountId = account.Id,
            }
        );

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permission1 = await permissionRepo.CreateAsync(
            new PermissionEntity
            {
                Id = Guid.CreateVersion7(),
                Description = "Read users",
                AccountId = account.Id,
                ResourceType = "User",
                ResourceId = Guid.Empty,
            }
        );

        role.Users = [user1, user2];
        role.Groups = [group1];
        role.Permissions = [permission1];

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        await roleRepo.UpdateAsync(role);

        return role;
    }

    [Fact]
    public async Task ListRolesAsync_ReturnsPaginatedResults()
    {
        await CreateRoleAsync("Role1");

        var result = await _roleProcessor.ListRolesAsync(null, null, 0, 25);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.Items.Count >= 1);
        Assert.True(result.Data.TotalCount >= 1);
    }

    [Fact]
    public async Task ListRolesAsync_ScopesToAccount()
    {
        await CreateRoleAsync("ScopedRole");

        // Create a role in a different account
        var otherAccountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var otherAccount = await otherAccountRepo.CreateAsync(
            new AccountEntity { Id = Guid.CreateVersion7(), AccountName = "OtherAccount" }
        );
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        await roleRepo.CreateAsync(
            new RoleEntity
            {
                Id = Guid.CreateVersion7(),
                Name = "OtherRole",
                AccountId = otherAccount.Id,
                Account = otherAccount,
            }
        );

        var result = await _roleProcessor.ListRolesAsync(null, null, 0, 100);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        // Only roles from the user's account should be returned
        Assert.All(result.Data.Items, role => Assert.Equal(_accountId, role.AccountId));
        Assert.DoesNotContain(result.Data.Items, role => role.Name == "OtherRole");
    }

    [Fact]
    public async Task ListRolesAsync_ReturnsUnauthorized_WhenNoAccountContext()
    {
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns((UserContext?)null);

        var result = await _roleProcessor.ListRolesAsync(null, null, 0, 25);

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetRoleByIdAsync_ReturnsRole_WhenFound()
    {
        var (role, _) = await CreateRoleAsync();

        var result = await _roleProcessor.GetRoleByIdAsync(role.Id, hydrate: false);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(role.Id, result.Data.Id);
        Assert.Equal("TestRole", result.Data.Name);
        Assert.Null(result.Data.Users);
        Assert.Null(result.Data.Groups);
        Assert.Null(result.Data.Permissions);
    }

    [Fact]
    public async Task GetRoleByIdAsync_ReturnsNotFoundError_WhenNotFound()
    {
        var result = await _roleProcessor.GetRoleByIdAsync(Guid.CreateVersion7(), hydrate: false);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetRoleByIdAsync_HydratesUsersGroupsAndPermissions_WhenHydrateTrue()
    {
        var role = await CreateRoleWithChildrenAsync();

        var result = await _roleProcessor.GetRoleByIdAsync(role.Id, hydrate: true);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Users);
        Assert.Equal(2, result.Data.Users.Count);
        Assert.Contains(result.Data.Users, u => u.Name == "user1");
        Assert.Contains(result.Data.Users, u => u.Name == "user2");
        Assert.NotNull(result.Data.Groups);
        Assert.Single(result.Data.Groups);
        Assert.Contains(result.Data.Groups, g => g.Name == "Group1");
        Assert.NotNull(result.Data.Permissions);
        Assert.Single(result.Data.Permissions);
        Assert.Contains(result.Data.Permissions, p => p.Name == "Read users");
    }
}
