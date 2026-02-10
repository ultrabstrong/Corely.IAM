using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Permissions.Processors;

public class PermissionProcessorListGetTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly PermissionProcessor _permissionProcessor;
    private readonly Guid _accountId = Guid.CreateVersion7();

    public PermissionProcessorListGetTests()
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

        _permissionProcessor = new PermissionProcessor(
            _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<ILogger<PermissionProcessor>>()
        );
    }

    private async Task<PermissionEntity> CreatePermissionEntityAsync(
        string resourceType,
        Guid? accountId = null,
        bool read = true,
        List<RoleEntity>? roles = null
    )
    {
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permission = new PermissionEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId ?? _accountId,
            ResourceType = resourceType,
            ResourceId = Guid.Empty,
            Read = read,
            Roles = roles ?? [],
        };
        return await permissionRepo.CreateAsync(permission);
    }

    private async Task<RoleEntity> CreateRoleEntityAsync(string name)
    {
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var role = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            AccountId = _accountId,
            Permissions = [],
        };
        return await roleRepo.CreateAsync(role);
    }

    [Fact]
    public async Task ListPermissionsAsync_ReturnsPagedResults()
    {
        await CreatePermissionEntityAsync(PermissionConstants.GROUP_RESOURCE_TYPE);
        await CreatePermissionEntityAsync(PermissionConstants.ROLE_RESOURCE_TYPE);
        await CreatePermissionEntityAsync(PermissionConstants.USER_RESOURCE_TYPE);

        var result = await _permissionProcessor.ListPermissionsAsync(null, null, 0, 10);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Equal(3, result.Data.Items.Count);
    }

    [Fact]
    public async Task ListPermissionsAsync_ScopesToAccount()
    {
        var otherAccountId = Guid.CreateVersion7();
        await CreatePermissionEntityAsync(PermissionConstants.GROUP_RESOURCE_TYPE);
        await CreatePermissionEntityAsync(
            PermissionConstants.ROLE_RESOURCE_TYPE,
            accountId: otherAccountId
        );

        var result = await _permissionProcessor.ListPermissionsAsync(null, null, 0, 10);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal(PermissionConstants.GROUP_RESOURCE_TYPE, result.Data.Items[0].ResourceType);
    }

    [Fact]
    public async Task ListPermissionsAsync_AppliesPaging()
    {
        await CreatePermissionEntityAsync(PermissionConstants.GROUP_RESOURCE_TYPE);
        await CreatePermissionEntityAsync(PermissionConstants.ROLE_RESOURCE_TYPE);
        await CreatePermissionEntityAsync(PermissionConstants.USER_RESOURCE_TYPE);

        var result = await _permissionProcessor.ListPermissionsAsync(null, null, 0, 2);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Equal(2, result.Data.Items.Count);
        Assert.True(result.Data.HasMore);
    }

    [Fact]
    public async Task ListPermissionsAsync_ReturnsEmptyWhenNoPermissions()
    {
        var result = await _permissionProcessor.ListPermissionsAsync(null, null, 0, 10);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.TotalCount);
        Assert.Empty(result.Data.Items);
    }

    [Fact]
    public async Task GetPermissionByIdAsync_ReturnsPermissionWhenFound()
    {
        var permission = await CreatePermissionEntityAsync(PermissionConstants.GROUP_RESOURCE_TYPE);

        var result = await _permissionProcessor.GetPermissionByIdAsync(
            permission.Id,
            hydrate: false
        );

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(permission.Id, result.Data.Id);
        Assert.Equal(PermissionConstants.GROUP_RESOURCE_TYPE, result.Data.ResourceType);
        Assert.Null(result.Data.Roles);
    }

    [Fact]
    public async Task GetPermissionByIdAsync_ReturnsNotFoundWhenPermissionDoesNotExist()
    {
        var result = await _permissionProcessor.GetPermissionByIdAsync(
            Guid.CreateVersion7(),
            hydrate: false
        );

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetPermissionByIdAsync_HydratesRoles()
    {
        var role = await CreateRoleEntityAsync("TestRole");
        var permission = await CreatePermissionEntityAsync(
            PermissionConstants.GROUP_RESOURCE_TYPE,
            roles: [role]
        );

        var result = await _permissionProcessor.GetPermissionByIdAsync(
            permission.Id,
            hydrate: true
        );

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Equal(permission.Id, result.Data.Id);

        Assert.NotNull(result.Data.Roles);
        Assert.Single(result.Data.Roles);
        Assert.Equal(role.Id, result.Data.Roles[0].Id);
        Assert.Equal("TestRole", result.Data.Roles[0].Name);
    }

    [Fact]
    public async Task GetPermissionByIdAsync_ReturnsEmptyRolesWhenHydratedWithNoRoles()
    {
        var permission = await CreatePermissionEntityAsync(PermissionConstants.GROUP_RESOURCE_TYPE);

        var result = await _permissionProcessor.GetPermissionByIdAsync(
            permission.Id,
            hydrate: true
        );

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Roles);
        Assert.Empty(result.Data.Roles);
    }
}
