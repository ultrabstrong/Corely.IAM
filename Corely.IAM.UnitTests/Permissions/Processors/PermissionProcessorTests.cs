using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
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
    public async Task CreateDefaultSystemPermissionsAsync_CreatesDefaultPermissions()
    {
        var accountId = await CreateAccountAsync();

        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(accountId);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissions = await permissionRepo.ListAsync(p => p.AccountId == accountId);
        Assert.Equal(5, permissions.Count);
        Assert.Contains(permissions, p => p.ResourceType == PermissionConstants.USER_RESOURCE_TYPE);
        Assert.Contains(
            permissions,
            p => p.ResourceType == PermissionConstants.ACCOUNT_RESOURCE_TYPE
        );
        Assert.Contains(
            permissions,
            p => p.ResourceType == PermissionConstants.GROUP_RESOURCE_TYPE
        );
        Assert.Contains(permissions, p => p.ResourceType == PermissionConstants.ROLE_RESOURCE_TYPE);
        Assert.Contains(
            permissions,
            p => p.ResourceType == PermissionConstants.PERMISSION_RESOURCE_TYPE
        );
    }
}
