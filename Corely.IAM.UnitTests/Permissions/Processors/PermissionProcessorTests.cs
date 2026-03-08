using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Users.Providers;
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
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<ILogger<PermissionProcessor>>()
        );
    }

    private async Task<AccountEntity> CreateAccountAsync()
    {
        var account = new AccountEntity { Id = Guid.CreateVersion7(), Permissions = [] };
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var created = await accountRepo.CreateAsync(account);
        return created;
    }

    private async Task AddPermissionToAccountAsync(Guid permissionId, Guid accountId)
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var account = await accountRepo.GetAsync(a => a.Id == accountId);
        var permission = await permissionRepo.GetAsync(p => p.Id == permissionId);
        account!.Permissions ??= [];
        account.Permissions.Add(permission!);
        await accountRepo.UpdateAsync(account);
    }

    private async Task CreateDefaultRolesAsync(Guid accountId)
    {
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        await roleRepo.CreateAsync(
            new RoleEntity
            {
                AccountId = accountId,
                Name = RoleConstants.OWNER_ROLE_NAME,
                IsSystemDefined = true,
            }
        );
    }

    [Fact]
    public async Task CreatePermission_Fails_WhenAccountDoesNotExist()
    {
        var request = new CreatePermissionRequest(
            Guid.CreateVersion7(),
            PermissionConstants.GROUP_RESOURCE_TYPE,
            Guid.Empty,
            Read: true
        );

        var result = await _permissionProcessor.CreatePermissionAsync(request);

        Assert.Equal(CreatePermissionResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task CreatePermission_Fails_WhenPermissionExists()
    {
        var account = await CreateAccountAsync();
        var request = new CreatePermissionRequest(
            account.Id,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            Guid.Empty,
            Read: true
        );
        var firstResult = await _permissionProcessor.CreatePermissionAsync(request);
        await AddPermissionToAccountAsync(firstResult.CreatedId, account.Id);

        var result = await _permissionProcessor.CreatePermissionAsync(request);

        Assert.Equal(CreatePermissionResultCode.PermissionExistsError, result.ResultCode);
    }

    [Fact]
    public async Task CreatePermission_ReturnsCreatePermissionResult()
    {
        var account = await CreateAccountAsync();
        var request = new CreatePermissionRequest(
            account.Id,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            Guid.Empty,
            Read: true
        );

        var result = await _permissionProcessor.CreatePermissionAsync(request);

        Assert.NotEqual(Guid.Empty, result.CreatedId);
        Assert.Equal(CreatePermissionResultCode.Success, result.ResultCode);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissionEntity = await permissionRepo.GetAsync(
            p => p.Id == result.CreatedId,
            include: q => q.Include(g => g.Account)
        );
        Assert.NotNull(permissionEntity);
        Assert.Equal(account.Id, permissionEntity.AccountId);
    }

    [Fact]
    public async Task CreateDefaultSystemPermissions_CreatesOnePermission()
    {
        var account = await CreateAccountAsync();
        await CreateDefaultRolesAsync(account.Id);

        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(account.Id);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissions = await permissionRepo.ListAsync(p => p.AccountId == account.Id);
        Assert.Single(permissions);
        Assert.All(
            permissions,
            p =>
            {
                Assert.NotEqual(Guid.Empty, p.Id);
                Assert.Equal(Guid.Empty, p.ResourceId);
                Assert.Equal(PermissionConstants.ALL_RESOURCE_TYPES, p.ResourceType);
            }
        );
        Assert.All(permissions, p => Assert.True(p.IsSystemDefined));
    }

    [Fact]
    public async Task CreateDefaultSystemPermissions_CreatesOwnerPermission_WithFullAccess()
    {
        var account = await CreateAccountAsync();
        await CreateDefaultRolesAsync(account.Id);

        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(account.Id);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissions = await permissionRepo.ListAsync(
            p => p.AccountId == account.Id,
            include: q => q.Include(p => p.Roles)
        );

        var ownerPermission = permissions.Single(p =>
            p.Create && p.Read && p.Update && p.Delete && p.Execute
        );
        Assert.NotNull(ownerPermission);
        Assert.Contains(ownerPermission.Roles!, r => r.Name == RoleConstants.OWNER_ROLE_NAME);
    }

    [Fact]
    public async Task DeletePermission_ReturnsSuccess_WhenPermissionExists()
    {
        var account = await CreateAccountAsync();
        var createRequest = new CreatePermissionRequest(
            account.Id,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            Guid.Empty,
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
    public async Task DeletePermission_ReturnsNotFound_WhenPermissionDoesNotExist()
    {
        var result = await _permissionProcessor.DeletePermissionAsync(Guid.CreateVersion7());

        Assert.Equal(DeletePermissionResultCode.PermissionNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DeletePermission_ReturnsSystemDefinedPermissionError_WhenPermissionIsSystemDefined()
    {
        var account = await CreateAccountAsync();
        await CreateDefaultRolesAsync(account.Id);
        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(account.Id);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var systemPermission = await permissionRepo.GetAsync(p =>
            p.AccountId == account.Id && p.IsSystemDefined
        );

        var result = await _permissionProcessor.DeletePermissionAsync(systemPermission!.Id);

        Assert.Equal(DeletePermissionResultCode.SystemDefinedPermissionError, result.ResultCode);
        Assert.Contains("system-defined", result.Message);

        // Verify permission still exists
        var permissionStillExists = await permissionRepo.GetAsync(p => p.Id == systemPermission.Id);
        Assert.NotNull(permissionStillExists);
    }

    [Fact]
    public async Task DeletePermission_ReturnsSystemDefinedPermissionError_ForAllSystemDefinedPermissions()
    {
        var account = await CreateAccountAsync();
        await CreateDefaultRolesAsync(account.Id);
        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(account.Id);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var systemPermissions = await permissionRepo.ListAsync(p =>
            p.AccountId == account.Id && p.IsSystemDefined
        );

        Assert.Single(systemPermissions);

        foreach (var permission in systemPermissions)
        {
            var result = await _permissionProcessor.DeletePermissionAsync(permission.Id);
            Assert.Equal(
                DeletePermissionResultCode.SystemDefinedPermissionError,
                result.ResultCode
            );
        }
    }
}
