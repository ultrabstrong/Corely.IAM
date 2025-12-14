using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.UnitTests.ClassData;
using Corely.IAM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Roles.Processors;

public class RoleProcessorTests
{
    private const string VALID_ROLE_NAME = "rolename";

    private readonly Fixture _fixture = new();
    private readonly ServiceFactory _serviceFactory = new();
    private readonly RoleProcessor _roleProcessor;

    public RoleProcessorTests()
    {
        _roleProcessor = new RoleProcessor(
            _serviceFactory.GetRequiredService<IRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<PermissionEntity>>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<RoleProcessor>>()
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

    private async Task<int> CreatePermissionAsync(
        int accountId,
        bool isSystemDefined = false,
        params int[] roleIds
    )
    {
        var permissionId = _fixture.Create<int>();
        var permission = new PermissionEntity
        {
            Id = permissionId,
            Roles = roleIds?.Select(r => new RoleEntity { Id = r })?.ToList() ?? [],
            AccountId = accountId,
            Account = new AccountEntity { Id = accountId },
            IsSystemDefined = isSystemDefined,
            ResourceType = "Test",
            ResourceId = 0,
        };
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var created = await permissionRepo.CreateAsync(permission);
        return created.Id;
    }

    private async Task<(int RoleId, int AccountId)> CreateRoleAsync()
    {
        var accountId = await CreateAccountAsync();
        var role = new RoleEntity
        {
            Name = VALID_ROLE_NAME,
            AccountId = accountId,
            Account = new AccountEntity { Id = accountId },
        };
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var created = await roleRepo.CreateAsync(role);
        return (created.Id, accountId);
    }

    [Fact]
    public async Task CreateRoleAsync_Fails_WhenAccountDoesNotExist()
    {
        var request = new CreateRoleRequest(VALID_ROLE_NAME, _fixture.Create<int>());

        var result = await _roleProcessor.CreateRoleAsync(request);

        Assert.Equal(CreateRoleResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task CreateRoleAsync_Fails_WhenRoleExists()
    {
        var request = new CreateRoleRequest(VALID_ROLE_NAME, await CreateAccountAsync());
        await _roleProcessor.CreateRoleAsync(request);

        var result = await _roleProcessor.CreateRoleAsync(request);

        Assert.Equal(CreateRoleResultCode.RoleExistsError, result.ResultCode);
    }

    [Fact]
    public async Task CreateRoleAsync_ReturnsCreateRoleResult()
    {
        var accountId = await CreateAccountAsync();
        var request = new CreateRoleRequest(VALID_ROLE_NAME, accountId);

        var result = await _roleProcessor.CreateRoleAsync(request);

        Assert.Equal(CreateRoleResultCode.Success, result.ResultCode);

        // Verify role is linked to account id
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var roleEntity = await roleRepo.GetAsync(
            r => r.Id == result.CreatedId,
            include: q => q.Include(r => r.Account)
        );
        Assert.NotNull(roleEntity);
        //Assert.NotNull(roleEntity.Account); // Account not available for memory mock repo
        Assert.Equal(accountId, roleEntity.AccountId);
    }

    [Fact]
    public async Task CreateRoleAsync_Throws_WithNullRequest()
    {
        var ex = Record.ExceptionAsync(() => _roleProcessor.CreateRoleAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(await ex);
    }

    [Theory, ClassData(typeof(NullEmptyAndWhitespace))]
    public async Task CreateRoleAsync_Throws_WhenRoleNameInvalid(string roleName)
    {
        var request = new CreateRoleRequest(roleName, await CreateAccountAsync());

        var ex = Record.ExceptionAsync(() => _roleProcessor.CreateRoleAsync(request));

        Assert.NotNull(ex);
        Assert.IsType<ValidationException>(await ex);
    }

    [Fact]
    public async Task CreateDefaultSystemRolesAsync_CreatesDefaultRoles()
    {
        var accountId = await CreateAccountAsync();

        await _roleProcessor.CreateDefaultSystemRolesAsync(accountId);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var roles = await roleRepo.ListAsync(r => r.AccountId == accountId);
        Assert.Equal(3, roles.Count);
        Assert.Contains(roles, r => r.Name == RoleConstants.OWNER_ROLE_NAME);
        Assert.Contains(roles, r => r.Name == RoleConstants.ADMIN_ROLE_NAME);
        Assert.Contains(roles, r => r.Name == RoleConstants.USER_ROLE_NAME);
    }

    [Fact]
    public async Task GetRoleByRoleIdAsync_ReturnsNull_WhenRoleNotFound()
    {
        var result = await _roleProcessor.GetRoleAsync(-1);
        Assert.Equal(GetRoleResultCode.RoleNotFoundError, result.ResultCode);
        Assert.Null(result.Role);
    }

    [Fact]
    public async Task GetRoleByRoleIdAsync_ReturnsRole_WhenRoleExists()
    {
        var accountId = await CreateAccountAsync();
        var request = new CreateRoleRequest(VALID_ROLE_NAME, accountId);
        var createResult = await _roleProcessor.CreateRoleAsync(request);

        var result = await _roleProcessor.GetRoleAsync(createResult.CreatedId);

        Assert.NotNull(result.Role);
        Assert.Equal(VALID_ROLE_NAME, result.Role!.Name);
    }

    [Fact]
    public async Task GetRoleByRoleNameAsync_ReturnsNull_WhenRoleNotFound()
    {
        var result = await _roleProcessor.GetRoleAsync("nonexistent", -1);
        Assert.Equal(GetRoleResultCode.RoleNotFoundError, result.ResultCode);
        Assert.Null(result.Role);
    }

    [Fact]
    public async Task GetRoleByRoleNameAsync_ReturnsRole_WhenRoleExists()
    {
        var accountId = await CreateAccountAsync();
        var request = new CreateRoleRequest(VALID_ROLE_NAME, accountId);
        await _roleProcessor.CreateRoleAsync(request);

        var result = await _roleProcessor.GetRoleAsync(VALID_ROLE_NAME, accountId);

        Assert.NotNull(result.Role);
        Assert.Equal(VALID_ROLE_NAME, result.Role!.Name);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_Fails_WhenRoleDoesNotExist()
    {
        var request = new AssignPermissionsToRoleRequest([], _fixture.Create<int>());
        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);
        Assert.Equal(AssignPermissionsToRoleResultCode.RoleNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_Fails_WhenPermissionsNotProvided()
    {
        var (roleId, _) = await CreateRoleAsync();
        var request = new AssignPermissionsToRoleRequest([], roleId);

        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);

        Assert.Equal(
            AssignPermissionsToRoleResultCode.InvalidPermissionIdsError,
            result.ResultCode
        );
        Assert.Equal(
            "All permission ids are invalid (not found, from different account, or already assigned to role)",
            result.Message
        );
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_Succeeds_WhenPermissionsAssigned()
    {
        var (roleId, accountId) = await CreateRoleAsync();
        var permissionId = await CreatePermissionAsync(accountId);
        var request = new AssignPermissionsToRoleRequest([permissionId], roleId);

        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);

        Assert.Equal(AssignPermissionsToRoleResultCode.Success, result.ResultCode);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var roleEntity = await roleRepo.GetAsync(
            r => r.Id == roleId,
            include: q => q.Include(r => r.Permissions)
        );

        Assert.NotNull(roleEntity);
        Assert.NotNull(roleEntity.Permissions);
        Assert.Contains(roleEntity.Permissions, p => p.Id == permissionId);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_PartiallySucceeds_WhenSomePermissionsExistForRole()
    {
        var (roleId, accountId) = await CreateRoleAsync();
        var existingPermissionId = await CreatePermissionAsync(
            accountId,
            isSystemDefined: false,
            roleId
        );
        var newPermissionId = await CreatePermissionAsync(
            accountId,
            isSystemDefined: false,
            roleId + 1
        );
        var request = new AssignPermissionsToRoleRequest(
            [existingPermissionId, newPermissionId],
            roleId
        );

        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);

        Assert.Equal(AssignPermissionsToRoleResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some permission ids are invalid (not found, from different account, or already assigned to role)",
            result.Message
        );
        Assert.Equal(1, result.AddedPermissionCount);
        Assert.NotEmpty(result.InvalidPermissionIds);
        Assert.Contains(existingPermissionId, result.InvalidPermissionIds);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_PartiallySucceeds_WhenSomePermissionsDoNotExist()
    {
        var (roleId, accountId) = await CreateRoleAsync();
        var permissionId = await CreatePermissionAsync(accountId);
        var request = new AssignPermissionsToRoleRequest([permissionId, -1], roleId);

        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);

        Assert.Equal(AssignPermissionsToRoleResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some permission ids are invalid (not found, from different account, or already assigned to role)",
            result.Message
        );
        Assert.NotEmpty(result.InvalidPermissionIds);
        Assert.Contains(-1, result.InvalidPermissionIds);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_PartiallySucceeds_WhenSomePermissionsBelongToDifferentAccount()
    {
        var (roleId, accountId) = await CreateRoleAsync();
        var permissionIdSameAccount = await CreatePermissionAsync(accountId);
        var permissionIdDifferentAccount = await CreatePermissionAsync(accountId + 1);
        var request = new AssignPermissionsToRoleRequest(
            [permissionIdSameAccount, permissionIdDifferentAccount],
            roleId
        );

        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);

        Assert.Equal(AssignPermissionsToRoleResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some permission ids are invalid (not found, from different account, or already assigned to role)",
            result.Message
        );
        Assert.Equal(1, result.AddedPermissionCount);
        Assert.NotEmpty(result.InvalidPermissionIds);
        Assert.Contains(permissionIdDifferentAccount, result.InvalidPermissionIds);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_Fails_WhenAllPermissionsExistForRole()
    {
        var (roleId, accountId) = await CreateRoleAsync();
        var permissionIds = new List<int>
        {
            await CreatePermissionAsync(accountId, isSystemDefined: false, roleId),
            await CreatePermissionAsync(accountId, isSystemDefined: false, roleId),
        };
        var request = new AssignPermissionsToRoleRequest(permissionIds, roleId);
        await _roleProcessor.AssignPermissionsToRoleAsync(request);

        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);

        Assert.Equal(
            AssignPermissionsToRoleResultCode.InvalidPermissionIdsError,
            result.ResultCode
        );
        Assert.Equal(
            "All permission ids are invalid (not found, from different account, or already assigned to role)",
            result.Message
        );
        Assert.Equal(permissionIds, result.InvalidPermissionIds);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_Fails_WhenAllRolesDoNotExist()
    {
        var (roleId, _) = await CreateRoleAsync();
        var permissionIds = _fixture.CreateMany<int>().ToList();
        var request = new AssignPermissionsToRoleRequest(permissionIds, roleId);

        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);

        Assert.Equal(
            AssignPermissionsToRoleResultCode.InvalidPermissionIdsError,
            result.ResultCode
        );
        Assert.Equal(
            "All permission ids are invalid (not found, from different account, or already assigned to role)",
            result.Message
        );
        Assert.Equal(0, result.AddedPermissionCount);
        Assert.Equal(permissionIds, result.InvalidPermissionIds);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_Fails_WhenAllPermissionsBelongToDifferentAccount()
    {
        var (roleId, accountId) = await CreateRoleAsync();
        var permissionIds = new List<int>()
        {
            await CreatePermissionAsync(accountId + 1),
            await CreatePermissionAsync(accountId + 2),
        };
        var request = new AssignPermissionsToRoleRequest(permissionIds, roleId);

        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);

        Assert.Equal(
            AssignPermissionsToRoleResultCode.InvalidPermissionIdsError,
            result.ResultCode
        );
        Assert.Equal(
            "All permission ids are invalid (not found, from different account, or already assigned to role)",
            result.Message
        );
        Assert.Equal(0, result.AddedPermissionCount);
        Assert.Equal(permissionIds, result.InvalidPermissionIds);
    }

    [Fact]
    public async Task DeleteRoleAsync_ReturnsSuccess_WhenRoleExists()
    {
        var (roleId, _) = await CreateRoleAsync();

        var result = await _roleProcessor.DeleteRoleAsync(roleId);

        Assert.Equal(DeleteRoleResultCode.Success, result.ResultCode);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var roleEntity = await roleRepo.GetAsync(r => r.Id == roleId);
        Assert.Null(roleEntity);
    }

    [Fact]
    public async Task DeleteRoleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        var result = await _roleProcessor.DeleteRoleAsync(_fixture.Create<int>());

        Assert.Equal(DeleteRoleResultCode.RoleNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DeleteRoleAsync_ReturnsSystemDefinedRoleError_WhenRoleIsSystemDefined()
    {
        var accountId = await CreateAccountAsync();
        await _roleProcessor.CreateDefaultSystemRolesAsync(accountId);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var ownerRole = await roleRepo.GetAsync(r =>
            r.AccountId == accountId && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var result = await _roleProcessor.DeleteRoleAsync(ownerRole!.Id);

        Assert.Equal(DeleteRoleResultCode.SystemDefinedRoleError, result.ResultCode);
        Assert.Contains("system-defined", result.Message);

        // Verify role still exists
        var roleStillExists = await roleRepo.GetAsync(r => r.Id == ownerRole.Id);
        Assert.NotNull(roleStillExists);
    }

    [Fact]
    public async Task DeleteRoleAsync_ReturnsSystemDefinedRoleError_ForAllSystemDefinedRoles()
    {
        var accountId = await CreateAccountAsync();
        await _roleProcessor.CreateDefaultSystemRolesAsync(accountId);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var systemRoles = await roleRepo.ListAsync(r =>
            r.AccountId == accountId && r.IsSystemDefined
        );

        Assert.Equal(3, systemRoles.Count);

        foreach (var role in systemRoles)
        {
            var result = await _roleProcessor.DeleteRoleAsync(role.Id);
            Assert.Equal(DeleteRoleResultCode.SystemDefinedRoleError, result.ResultCode);
        }
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_Fails_WhenRoleDoesNotExist()
    {
        var request = new RemovePermissionsFromRoleRequest([1, 2], _fixture.Create<int>());

        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        Assert.Equal(RemovePermissionsFromRoleResultCode.RoleNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_Fails_WhenPermissionsNotAssignedToRole()
    {
        var (roleId, accountId) = await CreateRoleAsync();
        var request = new RemovePermissionsFromRoleRequest([9999], roleId);

        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        Assert.Equal(
            RemovePermissionsFromRoleResultCode.InvalidPermissionIdsError,
            result.ResultCode
        );
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_Succeeds_WhenNonSystemRoleAndNonSystemPermission()
    {
        var (roleId, accountId) = await CreateRoleAsync();
        var permissionId = await CreatePermissionAsync(accountId, isSystemDefined: false, roleId);

        // Assign permission to role first
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var role = await roleRepo.GetAsync(r => r.Id == roleId);
        var permission = await permissionRepo.GetAsync(p => p.Id == permissionId);
        role!.Permissions = [permission!];
        await roleRepo.UpdateAsync(role);

        var request = new RemovePermissionsFromRoleRequest([permissionId], roleId);
        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        Assert.Equal(RemovePermissionsFromRoleResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedPermissionCount);
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_Succeeds_WhenNonSystemRoleAndSystemPermission()
    {
        var (roleId, accountId) = await CreateRoleAsync();
        var permissionId = await CreatePermissionAsync(accountId, isSystemDefined: true, roleId);

        // Assign permission to role first
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var role = await roleRepo.GetAsync(r => r.Id == roleId);
        var permission = await permissionRepo.GetAsync(p => p.Id == permissionId);
        role!.Permissions = [permission!];
        await roleRepo.UpdateAsync(role);

        var request = new RemovePermissionsFromRoleRequest([permissionId], roleId);
        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        // Non-system role can remove ANY permission
        Assert.Equal(RemovePermissionsFromRoleResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedPermissionCount);
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_Succeeds_WhenSystemRoleAndNonSystemPermission()
    {
        var accountId = await CreateAccountAsync();
        await _roleProcessor.CreateDefaultSystemRolesAsync(accountId);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var systemRole = await roleRepo.GetAsync(r =>
            r.AccountId == accountId && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var permissionId = await CreatePermissionAsync(accountId, isSystemDefined: false);

        // Assign non-system permission to system role
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permission = await permissionRepo.GetAsync(p => p.Id == permissionId);
        systemRole!.Permissions = [permission!];
        await roleRepo.UpdateAsync(systemRole);

        var request = new RemovePermissionsFromRoleRequest([permissionId], systemRole.Id);
        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        // System role CAN remove non-system permissions
        Assert.Equal(RemovePermissionsFromRoleResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedPermissionCount);
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_Fails_WhenSystemRoleAndSystemPermission()
    {
        var accountId = await CreateAccountAsync();
        await _roleProcessor.CreateDefaultSystemRolesAsync(accountId);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var systemRole = await roleRepo.GetAsync(r =>
            r.AccountId == accountId && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var permissionId = await CreatePermissionAsync(accountId, isSystemDefined: true);

        // Assign system permission to system role
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var permission = await permissionRepo.GetAsync(p => p.Id == permissionId);
        systemRole!.Permissions = [permission!];
        await roleRepo.UpdateAsync(systemRole);

        var request = new RemovePermissionsFromRoleRequest([permissionId], systemRole.Id);
        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        // System role CANNOT remove system permissions
        Assert.Equal(
            RemovePermissionsFromRoleResultCode.SystemPermissionRemovalError,
            result.ResultCode
        );
        Assert.Equal(0, result.RemovedPermissionCount);
        Assert.Contains(permissionId, result.SystemPermissionIds);
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_PartialSuccess_WhenSystemRoleAndMixedPermissions()
    {
        var accountId = await CreateAccountAsync();
        await _roleProcessor.CreateDefaultSystemRolesAsync(accountId);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var systemRole = await roleRepo.GetAsync(r =>
            r.AccountId == accountId && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var nonSystemPermissionId = await CreatePermissionAsync(accountId, isSystemDefined: false);
        var systemPermissionId = await CreatePermissionAsync(accountId, isSystemDefined: true);

        // Assign both permissions to system role
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var nonSystemPermission = await permissionRepo.GetAsync(p => p.Id == nonSystemPermissionId);
        var systemPermission = await permissionRepo.GetAsync(p => p.Id == systemPermissionId);
        systemRole!.Permissions = [nonSystemPermission!, systemPermission!];
        await roleRepo.UpdateAsync(systemRole);

        var request = new RemovePermissionsFromRoleRequest(
            [nonSystemPermissionId, systemPermissionId],
            systemRole.Id
        );
        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        // Should remove non-system but not system permission
        Assert.Equal(RemovePermissionsFromRoleResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(1, result.RemovedPermissionCount);
        Assert.Contains(systemPermissionId, result.SystemPermissionIds);
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _roleProcessor.RemovePermissionsFromRoleAsync(null!)
        );

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }
}
