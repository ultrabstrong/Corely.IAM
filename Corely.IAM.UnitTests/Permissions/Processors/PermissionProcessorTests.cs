using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Permissions.Providers;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Constants;
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
        var registry = new ResourceTypeRegistry();
        registry.Register("invoice", "Invoices", [AuthAction.Read, AuthAction.Update]);
        registry.Register("report", "Reports", []);

        _permissionProcessor = new PermissionProcessor(
            _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            registry,
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
    public async Task CreatePermission_NamesExistingPermission_ForDuplicateWithDescription()
    {
        var account = await CreateAccountAsync();
        var first = await _permissionProcessor.CreatePermissionAsync(
            new CreatePermissionRequest(
                account.Id,
                PermissionConstants.GROUP_RESOURCE_TYPE,
                Guid.Empty,
                Read: true,
                Description: "Read groups"
            )
        );
        await AddPermissionToAccountAsync(first.CreatedId, account.Id);

        var result = await _permissionProcessor.CreatePermissionAsync(
            new CreatePermissionRequest(
                account.Id,
                PermissionConstants.GROUP_RESOURCE_TYPE,
                Guid.Empty,
                Read: true,
                Description: "Another name"
            )
        );

        Assert.Equal(
            $"You already have this permission: \"Read groups\" ({first.CreatedId})",
            result.Message
        );
    }

    [Fact]
    public async Task CreatePermission_NamesExistingPermission_ForDuplicateWithoutDescription()
    {
        var account = await CreateAccountAsync();
        var request = new CreatePermissionRequest(
            account.Id,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            Guid.Empty,
            Read: true
        );
        var first = await _permissionProcessor.CreatePermissionAsync(request);
        await AddPermissionToAccountAsync(first.CreatedId, account.Id);

        var result = await _permissionProcessor.CreatePermissionAsync(request);

        Assert.Equal(
            $"You already have this permission: \"group - all cRudx\" ({first.CreatedId})",
            result.Message
        );
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
    public async Task CreateDefaultSystemPermissions_CreatesOneRowPerTypeWithOwnerActions_ForRegisteredTypes()
    {
        var account = await CreateAccountAsync();
        await CreateDefaultRolesAsync(account.Id);

        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(account.Id);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissions = await permissionRepo.ListAsync(p => p.AccountId == account.Id);
        Assert.Equal(
            [
                PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                PermissionConstants.GROUP_RESOURCE_TYPE,
                "invoice",
                PermissionConstants.PERMISSION_RESOURCE_TYPE,
                PermissionConstants.ROLE_RESOURCE_TYPE,
                PermissionConstants.USER_RESOURCE_TYPE,
            ],
            permissions.Select(p => p.ResourceType).Order()
        );
        Assert.All(
            permissions,
            p =>
            {
                Assert.NotEqual(Guid.Empty, p.Id);
                Assert.Equal(Guid.Empty, p.ResourceId);
                Assert.True(p.IsSystemDefined);
            }
        );
    }

    [Fact]
    public async Task CreateDefaultSystemPermissions_GivesEveryAction_ForIAMTypes()
    {
        var account = await CreateAccountAsync();
        await CreateDefaultRolesAsync(account.Id);

        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(account.Id);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissions = await permissionRepo.ListAsync(
            p => p.AccountId == account.Id && p.ResourceType != "invoice",
            include: q => q.Include(p => p.Roles)
        );

        Assert.Equal(5, permissions.Count);
        Assert.All(
            permissions,
            p =>
            {
                Assert.True(p.Create && p.Read && p.Update && p.Delete && p.Execute);
                Assert.Contains(p.Roles!, r => r.Name == RoleConstants.OWNER_ROLE_NAME);
            }
        );
    }

    [Fact]
    public async Task CreateDefaultSystemPermissions_GivesOnlyRegisteredOwnerActions_ForHostType()
    {
        var account = await CreateAccountAsync();
        await CreateDefaultRolesAsync(account.Id);

        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(account.Id);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var invoice = await permissionRepo.GetAsync(
            p => p.AccountId == account.Id && p.ResourceType == "invoice",
            include: q => q.Include(p => p.Roles)
        );

        Assert.NotNull(invoice);
        Assert.False(invoice.Create);
        Assert.True(invoice.Read);
        Assert.True(invoice.Update);
        Assert.False(invoice.Delete);
        Assert.False(invoice.Execute);
        Assert.Contains(invoice.Roles!, r => r.Name == RoleConstants.OWNER_ROLE_NAME);
    }

    [Fact]
    public async Task CreateDefaultSystemPermissions_DescribesTypeNotRole_ForEachRow()
    {
        var account = await CreateAccountAsync();
        await CreateDefaultRolesAsync(account.Id);

        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(account.Id);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var invoice = await permissionRepo.GetAsync(p =>
            p.AccountId == account.Id && p.ResourceType == "invoice"
        );
        Assert.Equal("Invoices", invoice!.Description);
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

        Assert.Equal(6, systemPermissions.Count);

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
