using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Permissions.Processors;

public class PermissionProcessorTests
{
    private readonly Fixture _fixture = new();
    private readonly ServiceFactory _serviceFactory = new();
    private readonly PermissionProcessor _permissionProcessor;

    public PermissionProcessorTests()
    {
        _permissionProcessor = new PermissionProcessor(
            _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<PermissionProcessor>>()
        );
    }

    private async Task<int> CreateAccountAsync()
    {
        var accountId = _fixture.Create<int>();
        var account = new AccountEntity { Id = accountId };
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var created = await accountRepo.CreateAsync(account);
        return created.Id;
    }

    private async Task CreateDefaultRolesAsync(int accountId)
    {
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        RoleEntity[] roles =
        [
            new()
            {
                AccountId = accountId,
                Name = RoleConstants.OWNER_ROLE_NAME,
                IsSystemDefined = true,
            },
            new()
            {
                AccountId = accountId,
                Name = RoleConstants.ADMIN_ROLE_NAME,
                IsSystemDefined = true,
            },
            new()
            {
                AccountId = accountId,
                Name = RoleConstants.USER_ROLE_NAME,
                IsSystemDefined = true,
            },
        ];
        await roleRepo.CreateAsync(roles);
    }

    [Fact]
    public async Task CreatePermissionAsync_Fails_WhenAccountDoesNotExist()
    {
        var request = new CreatePermissionRequest(
            _fixture.Create<int>(),
            PermissionConstants.GROUP_RESOURCE_TYPE,
            0,
            Read: true
        );

        var result = await _permissionProcessor.CreatePermissionAsync(request);

        Assert.Equal(CreatePermissionResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task CreatePermissionAsync_Fails_WhenPermissionExists()
    {
        var accountId = await CreateAccountAsync();
        var request = new CreatePermissionRequest(
            accountId,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            0,
            Read: true
        );
        await _permissionProcessor.CreatePermissionAsync(request);

        var result = await _permissionProcessor.CreatePermissionAsync(request);

        Assert.Equal(CreatePermissionResultCode.PermissionExistsError, result.ResultCode);
    }

    [Fact]
    public async Task CreatePermissionAsync_ReturnsCreatePermissionResult()
    {
        var accountId = await CreateAccountAsync();
        var request = new CreatePermissionRequest(
            accountId,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            0,
            Read: true
        );

        var result = await _permissionProcessor.CreatePermissionAsync(request);

        Assert.Equal(CreatePermissionResultCode.Success, result.ResultCode);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissionEntity = await permissionRepo.GetAsync(
            p => p.Id == result.CreatedId,
            include: q => q.Include(g => g.Account)
        );
        Assert.NotNull(permissionEntity);
        Assert.Equal(accountId, permissionEntity.AccountId);
    }

    [Fact]
    public async Task CreateDefaultSystemPermissionsAsync_CreatesThreePermissions()
    {
        var accountId = await CreateAccountAsync();
        await CreateDefaultRolesAsync(accountId);

        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(accountId);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissions = await permissionRepo.ListAsync(p => p.AccountId == accountId);
        Assert.Equal(3, permissions.Count);
        Assert.All(
            permissions,
            p => Assert.Equal(PermissionConstants.ALL_RESOURCE_TYPES, p.ResourceType)
        );
        Assert.All(permissions, p => Assert.True(p.IsSystemDefined));
    }

    [Fact]
    public async Task CreateDefaultSystemPermissionsAsync_CreatesOwnerPermission_WithFullAccess()
    {
        var accountId = await CreateAccountAsync();
        await CreateDefaultRolesAsync(accountId);

        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(accountId);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissions = await permissionRepo.ListAsync(
            p => p.AccountId == accountId,
            include: q => q.Include(p => p.Roles)
        );

        var ownerPermission = permissions.Single(p =>
            p.Create && p.Read && p.Update && p.Delete && p.Execute
        );
        Assert.NotNull(ownerPermission);
        Assert.Contains(ownerPermission.Roles!, r => r.Name == RoleConstants.OWNER_ROLE_NAME);
    }

    [Fact]
    public async Task CreateDefaultSystemPermissionsAsync_CreatesAdminPermission_WithoutDelete()
    {
        var accountId = await CreateAccountAsync();
        await CreateDefaultRolesAsync(accountId);

        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(accountId);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissions = await permissionRepo.ListAsync(
            p => p.AccountId == accountId,
            include: q => q.Include(p => p.Roles)
        );

        var adminPermission = permissions.Single(p =>
            p.Create && p.Read && p.Update && !p.Delete && p.Execute
        );
        Assert.NotNull(adminPermission);
        Assert.Contains(adminPermission.Roles!, r => r.Name == RoleConstants.ADMIN_ROLE_NAME);
    }

    [Fact]
    public async Task CreateDefaultSystemPermissionsAsync_CreatesUserPermission_ReadOnly()
    {
        var accountId = await CreateAccountAsync();
        await CreateDefaultRolesAsync(accountId);

        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(accountId);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissions = await permissionRepo.ListAsync(
            p => p.AccountId == accountId,
            include: q => q.Include(p => p.Roles)
        );

        var userPermission = permissions.Single(p =>
            !p.Create && p.Read && !p.Update && !p.Delete && !p.Execute
        );
        Assert.NotNull(userPermission);
        Assert.Contains(userPermission.Roles!, r => r.Name == RoleConstants.USER_ROLE_NAME);
    }

    [Fact]
    public async Task DeletePermissionAsync_ReturnsSuccess_WhenPermissionExists()
    {
        var accountId = await CreateAccountAsync();
        var createRequest = new CreatePermissionRequest(
            accountId,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            0,
            Read: true
        );
        var createResult = await _permissionProcessor.CreatePermissionAsync(createRequest);

        var result = await _permissionProcessor.DeletePermissionAsync(createResult.CreatedId);

        Assert.Equal(DeletePermissionResultCode.Success, result.ResultCode);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissionEntity = await permissionRepo.GetAsync(p => p.Id == createResult.CreatedId);
        Assert.Null(permissionEntity);
    }

    [Fact]
    public async Task DeletePermissionAsync_ReturnsNotFound_WhenPermissionDoesNotExist()
    {
        var result = await _permissionProcessor.DeletePermissionAsync(_fixture.Create<int>());

        Assert.Equal(DeletePermissionResultCode.PermissionNotFoundError, result.ResultCode);
    }
}
