using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Audits.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Permissions.Providers;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Models;
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
                Name = Role.OWNER_ROLE_NAME,
                IsSystemDefined = true,
            }
        );
    }

    [Fact]
    public async Task CreatePermission_Fails_WhenAccountDoesNotExist()
    {
        var request = new CreatePermissionRequest(
            Guid.CreateVersion7(),
            Group.RESOURCE_TYPE,
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
            Group.RESOURCE_TYPE,
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
                Group.RESOURCE_TYPE,
                Guid.Empty,
                Read: true,
                Description: "Read groups"
            )
        );
        await AddPermissionToAccountAsync(first.CreatedId, account.Id);

        var result = await _permissionProcessor.CreatePermissionAsync(
            new CreatePermissionRequest(
                account.Id,
                Group.RESOURCE_TYPE,
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
            Group.RESOURCE_TYPE,
            Guid.Empty,
            Read: true
        );
        var first = await _permissionProcessor.CreatePermissionAsync(request);
        await AddPermissionToAccountAsync(first.CreatedId, account.Id);

        var result = await _permissionProcessor.CreatePermissionAsync(request);

        Assert.Equal(
            $"You already have this permission: \"Group : Read\" ({first.CreatedId})",
            result.Message
        );
    }

    [Fact]
    public async Task CreatePermission_ReturnsCreatePermissionResult()
    {
        var account = await CreateAccountAsync();
        var request = new CreatePermissionRequest(
            account.Id,
            Group.RESOURCE_TYPE,
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
                Account.RESOURCE_TYPE,
                Account.SECRET_KEYS_RESOURCE_TYPE,
                ApiKey.RESOURCE_TYPE,
                AuditEntry.RESOURCE_TYPE,
                AccountAuditSettings.RESOURCE_TYPE,
                Group.RESOURCE_TYPE,
                "invoice",
                Permission.RESOURCE_TYPE,
                Role.RESOURCE_TYPE,
                User.RESOURCE_TYPE,
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
            p =>
                p.AccountId == account.Id
                && p.ResourceType != "invoice"
                && p.ResourceType != Account.SECRET_KEYS_RESOURCE_TYPE
                && p.ResourceType != AuditEntry.RESOURCE_TYPE
                && p.ResourceType != AccountAuditSettings.RESOURCE_TYPE,
            include: q => q.Include(p => p.Roles)
        );

        Assert.Equal(6, permissions.Count);
        Assert.All(
            permissions,
            p =>
            {
                Assert.True(p.Create && p.Read && p.Update && p.Delete && p.Execute);
                Assert.Contains(p.Roles!, r => r.Name == Role.OWNER_ROLE_NAME);
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
        Assert.Contains(invoice.Roles!, r => r.Name == Role.OWNER_ROLE_NAME);
    }

    [Fact]
    public async Task CreateDefaultSystemPermissions_DescribesTypeAndActions_ForEachRow()
    {
        var account = await CreateAccountAsync();
        await CreateDefaultRolesAsync(account.Id);

        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(account.Id);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var invoice = await permissionRepo.GetAsync(p =>
            p.AccountId == account.Id && p.ResourceType == "invoice"
        );
        Assert.Equal("Invoice : Read & Update", invoice!.Description);
    }

    private PermissionProcessor ProcessorWithTypes(
        params (string Name, AuthAction[] OwnerActions)[] types
    )
    {
        var registry = new ResourceTypeRegistry();
        foreach (var (name, ownerActions) in types)
            registry.Register(name, name, ownerActions);
        return new PermissionProcessor(
            _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            registry,
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<ILogger<PermissionProcessor>>()
        );
    }

    private async Task<AccountEntity> CreateSeededAccountAsync()
    {
        var account = await CreateAccountAsync();
        await CreateDefaultRolesAsync(account.Id);
        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(account.Id);
        return account;
    }

    private Task<List<PermissionEntity>> ListPermissionsAsync(
        Guid accountId,
        string resourceType
    ) =>
        _serviceFactory
            .GetRequiredService<IRepo<PermissionEntity>>()
            .ListAsync(
                p => p.AccountId == accountId && p.ResourceType == resourceType,
                include: q => q.Include(p => p.Roles)
            );

    [Fact]
    public async Task CreateMissingOwnerDefaults_GivesExistingOwnerRolesANewlyRegisteredType()
    {
        var account = await CreateSeededAccountAsync();
        var processor = ProcessorWithTypes(
            ("invoice", [AuthAction.Read, AuthAction.Update]),
            ("quota", [AuthAction.Read, AuthAction.Execute])
        );

        var added = await processor.CreateMissingOwnerDefaultsAsync();

        Assert.Equal(1, added);
        var quota = Assert.Single(await ListPermissionsAsync(account.Id, "quota"));
        Assert.True(quota.IsSystemDefined);
        Assert.Equal(Guid.Empty, quota.ResourceId);
        Assert.True(quota.Read && quota.Execute);
        Assert.False(quota.Create || quota.Update || quota.Delete);
        Assert.Equal("Quota : Read & Execute", quota.Description);
        Assert.Contains(quota.Roles!, r => r.Name == Role.OWNER_ROLE_NAME);
    }

    [Fact]
    public async Task CreateMissingOwnerDefaults_AddsNothing_WhenRunAgain()
    {
        await CreateSeededAccountAsync();
        var processor = ProcessorWithTypes(("quota", [AuthAction.Read]));
        await processor.CreateMissingOwnerDefaultsAsync();

        var added = await processor.CreateMissingOwnerDefaultsAsync();

        Assert.Equal(0, added);
    }

    [Fact]
    public async Task CreateMissingOwnerDefaults_LeavesAnExistingTypesRowAlone_WhenItsDefaultChanged()
    {
        var account = await CreateSeededAccountAsync();
        var processor = ProcessorWithTypes(
            ("invoice", [AuthAction.Create, AuthAction.Read, AuthAction.Update, AuthAction.Delete])
        );

        var added = await processor.CreateMissingOwnerDefaultsAsync();

        Assert.Equal(0, added);
        var invoice = Assert.Single(await ListPermissionsAsync(account.Id, "invoice"));
        Assert.False(invoice.Create || invoice.Delete);
        Assert.True(invoice.Read && invoice.Update);
    }

    [Fact]
    public async Task CreateMissingOwnerDefaults_AdoptsAnIdenticalPermission_InsteadOfDuplicatingIt()
    {
        var account = await CreateSeededAccountAsync();
        var existing = await _serviceFactory
            .GetRequiredService<IRepo<PermissionEntity>>()
            .CreateAsync(
                new PermissionEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = account.Id,
                    ResourceType = "report",
                    ResourceId = Guid.Empty,
                    Read = true,
                    Roles = [],
                }
            );
        var processor = ProcessorWithTypes(("report", [AuthAction.Read]));

        var added = await processor.CreateMissingOwnerDefaultsAsync();

        Assert.Equal(1, added);
        var report = Assert.Single(await ListPermissionsAsync(account.Id, "report"));
        Assert.Equal(existing.Id, report.Id);
        Assert.True(report.IsSystemDefined);
        Assert.Contains(report.Roles!, r => r.Name == Role.OWNER_ROLE_NAME);
    }

    [Fact]
    public async Task CreateMissingOwnerDefaults_SkipsThePlatformAccount()
    {
        var account = await CreateSeededAccountAsync();
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var platform = await accountRepo.GetAsync(a => a.Id == account.Id);
        platform!.IsPlatformAccount = true;
        await accountRepo.UpdateAsync(platform);
        var processor = ProcessorWithTypes(("quota", [AuthAction.Read]));

        var added = await processor.CreateMissingOwnerDefaultsAsync();

        Assert.Equal(0, added);
        Assert.Empty(await ListPermissionsAsync(account.Id, "quota"));
    }

    [Fact]
    public async Task DeletePermission_ReturnsSuccess_WhenPermissionExists()
    {
        var account = await CreateAccountAsync();
        var createRequest = new CreatePermissionRequest(
            account.Id,
            Group.RESOURCE_TYPE,
            Guid.Empty,
            Read: true
        );
        var createResult = await _permissionProcessor.CreatePermissionAsync(createRequest);

        var result = await _permissionProcessor.DeletePermissionAsync(
            createResult.CreatedId,
            AccountScope.Any
        );

        Assert.Equal(DeletePermissionResultCode.Success, result.ResultCode);

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permissionEntity = await permissionRepo.GetAsync(p => p.Id == createResult.CreatedId);
        Assert.Null(permissionEntity);
    }

    [Fact]
    public async Task DeletePermission_ReturnsNotFound_WhenPermissionDoesNotExist()
    {
        var result = await _permissionProcessor.DeletePermissionAsync(
            Guid.CreateVersion7(),
            AccountScope.Any
        );

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

        var result = await _permissionProcessor.DeletePermissionAsync(
            systemPermission!.Id,
            AccountScope.Any
        );

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

        Assert.Equal(10, systemPermissions.Count);

        foreach (var permission in systemPermissions)
        {
            var result = await _permissionProcessor.DeletePermissionAsync(
                permission.Id,
                AccountScope.Any
            );
            Assert.Equal(
                DeletePermissionResultCode.SystemDefinedPermissionError,
                result.ResultCode
            );
        }
    }

    [Fact]
    public async Task UpdatePermission_ChangesOnlyDescription_ForSystemDefinedPermission()
    {
        var account = await CreateAccountAsync();
        await CreateDefaultRolesAsync(account.Id);
        await _permissionProcessor.CreateDefaultSystemPermissionsAsync(account.Id);
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var invoice = await permissionRepo.GetAsync(p =>
            p.AccountId == account.Id && p.ResourceType == "invoice"
        );

        var result = await _permissionProcessor.UpdatePermissionAsync(
            new UpdatePermissionRequest(invoice!.Id, account.Id, "  Invoice editing  ")
        );

        var updated = await permissionRepo.GetAsync(p => p.Id == invoice.Id);
        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        Assert.Equal("Invoice editing", updated!.Description);
        Assert.True(updated.IsSystemDefined);
        Assert.True(updated.Read && updated.Update);
        Assert.False(updated.Create || updated.Delete || updated.Execute);
    }

    [Fact]
    public async Task UpdatePermission_ClearsDescription_ForBlankDescription()
    {
        var account = await CreateAccountAsync();
        var created = await _permissionProcessor.CreatePermissionAsync(
            new CreatePermissionRequest(
                account.Id,
                Group.RESOURCE_TYPE,
                Guid.Empty,
                Read: true,
                Description: "Read groups"
            )
        );
        await AddPermissionToAccountAsync(created.CreatedId, account.Id);

        await _permissionProcessor.UpdatePermissionAsync(
            new UpdatePermissionRequest(created.CreatedId, account.Id, " ")
        );

        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        Assert.Null((await permissionRepo.GetAsync(p => p.Id == created.CreatedId))!.Description);
    }

    [Fact]
    public async Task UpdatePermission_ReturnsNotFound_ForPermissionInAnotherAccount()
    {
        var account = await CreateAccountAsync();
        var other = await CreateAccountAsync();
        var created = await _permissionProcessor.CreatePermissionAsync(
            new CreatePermissionRequest(account.Id, Group.RESOURCE_TYPE, Guid.Empty, Read: true)
        );
        await AddPermissionToAccountAsync(created.CreatedId, account.Id);

        var result = await _permissionProcessor.UpdatePermissionAsync(
            new UpdatePermissionRequest(created.CreatedId, other.Id, "Renamed")
        );

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }
}
