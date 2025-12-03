using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Groups.Processors;
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
    private const string VALID_PERMISSION_NAME = "permissionname";

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
        var request = _fixture
            .Build<CreatePermissionRequest>()
            .With(r => r.PermissionName, VALID_PERMISSION_NAME)
            .Create();

        var result = await _permissionProcessor.CreatePermissionAsync(request);

        Assert.Equal(CreatePermissionResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task CreatePermissionAsync_Fails_WhenPermissionExists()
    {
        var request = _fixture
            .Build<CreatePermissionRequest>()
            .With(r => r.PermissionName, VALID_PERMISSION_NAME)
            .With(r => r.OwnerAccountId, await CreateAccountAsync())
            .Create();
        await _permissionProcessor.CreatePermissionAsync(request);

        var result = await _permissionProcessor.CreatePermissionAsync(request);

        Assert.Equal(CreatePermissionResultCode.PermissionExistsError, result.ResultCode);
    }

    [Fact]
    public async Task CreatePermissionAsync_ReturnsCreatePermissionResult()
    {
        var accountId = await CreateAccountAsync();
        var request = _fixture
            .Build<CreatePermissionRequest>()
            .With(r => r.PermissionName, VALID_PERMISSION_NAME)
            .With(r => r.OwnerAccountId, accountId)
            .Create();

        var result = await _permissionProcessor.CreatePermissionAsync(request);

        Assert.Equal(CreatePermissionResultCode.Success, result.ResultCode);

        // Verify permission is linked to account id
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissionEntity = await permissionRepo.GetAsync(
            p => p.Id == result.CreatedId,
            include: q => q.Include(g => g.Account)
        );
        Assert.NotNull(permissionEntity);
        // Assert.NotNull(permissionEntity.Account); // Account not available for memory mock repo
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
        Assert.Contains(
            permissions,
            p => p.Name == PermissionConstants.ADMIN_USER_ACCESS_PERMISSION_NAME
        );
        Assert.Contains(
            permissions,
            p => p.Name == PermissionConstants.ADMIN_ACCOUNT_ACCESS_PERMISSION_NAME
        );
        Assert.Contains(
            permissions,
            p => p.Name == PermissionConstants.ADMIN_GROUP_ACCESS_PERMISSION_NAME
        );
        Assert.Contains(
            permissions,
            p => p.Name == PermissionConstants.ADMIN_ROLE_ACCESS_PERMISSION_NAME
        );
        Assert.Contains(
            permissions,
            p => p.Name == PermissionConstants.ADMIN_PERMISSION_ACCESS_PERMISSION_NAME
        );
    }
}
