using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
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

    private async Task<AccountEntity> CreateAccountAsync()
    {
        var account = new AccountEntity { Id = Guid.CreateVersion7() };
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var created = await accountRepo.CreateAsync(account);
        return created;
    }

    private async Task<PermissionEntity> CreatePermissionAsync(
        Guid accountId,
        bool isSystemDefined = false,
        params Guid[] roleIds
    )
    {
        var permission = new PermissionEntity
        {
            Id = Guid.CreateVersion7(),
            Roles = roleIds?.Select(r => new RoleEntity { Id = r })?.ToList() ?? [],
            AccountId = accountId,
            Account = new AccountEntity { Id = accountId },
            IsSystemDefined = isSystemDefined,
            ResourceType = "Test",
            ResourceId = Guid.Empty,
        };
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();
        var created = await permissionRepo.CreateAsync(permission);
        return created;
    }

    private async Task<(RoleEntity Role, AccountEntity Account)> CreateRoleAsync()
    {
        var account = await CreateAccountAsync();
        var role = new RoleEntity
        {
            Name = VALID_ROLE_NAME,
            AccountId = account.Id,
            Account = new AccountEntity { Id = account.Id },
        };
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var created = await roleRepo.CreateAsync(role);
        return (created, account);
    }

    [Fact]
    public async Task CreateRoleAsync_Fails_WhenAccountDoesNotExist()
    {
        var request = new CreateRoleRequest(VALID_ROLE_NAME, Guid.CreateVersion7());

        var result = await _roleProcessor.CreateRoleAsync(request);

        Assert.Equal(CreateRoleResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task CreateRoleAsync_Fails_WhenRoleExists()
    {
        var ownerAccount = await CreateAccountAsync();
        var request = new CreateRoleRequest(VALID_ROLE_NAME, ownerAccount.Id);
        await _roleProcessor.CreateRoleAsync(request);

        var result = await _roleProcessor.CreateRoleAsync(request);

        Assert.Equal(CreateRoleResultCode.RoleExistsError, result.ResultCode);
    }

    [Fact]
    public async Task CreateRoleAsync_ReturnsCreateRoleResult()
    {
        var ownerAccount = await CreateAccountAsync();
        var request = new CreateRoleRequest(VALID_ROLE_NAME, ownerAccount.Id);

        var result = await _roleProcessor.CreateRoleAsync(request);

        Assert.NotEqual(Guid.Empty, result.CreatedId);
        Assert.Equal(CreateRoleResultCode.Success, result.ResultCode);

        // Verify role is linked to account id
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var roleEntity = await roleRepo.GetAsync(
            r => r.Id == result.CreatedId,
            include: q => q.Include(r => r.Account)
        );
        Assert.NotNull(roleEntity);
        //Assert.NotNull(roleEntity.Account); // Account not available for memory mock repo
        Assert.Equal(ownerAccount.Id, roleEntity.AccountId);
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
        var ownerAccount = await CreateAccountAsync();
        var request = new CreateRoleRequest(roleName, ownerAccount.Id);

        var ex = Record.ExceptionAsync(() => _roleProcessor.CreateRoleAsync(request));

        Assert.NotNull(ex);
        Assert.IsType<ValidationException>(await ex);
    }

    [Fact]
    public async Task CreateDefaultSystemRolesAsync_CreatesDefaultRoles()
    {
        var ownerAccount = await CreateAccountAsync();

        var result = await _roleProcessor.CreateDefaultSystemRolesAsync(ownerAccount.Id);

        Assert.NotEqual(Guid.Empty, result.OwnerRoleId);
        Assert.NotEqual(Guid.Empty, result.AdminRoleId);
        Assert.NotEqual(Guid.Empty, result.UserRoleId);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var roles = await roleRepo.ListAsync(r => r.AccountId == ownerAccount.Id);
        Assert.Equal(3, roles.Count);
        Assert.Contains(roles, r => r.Name == RoleConstants.OWNER_ROLE_NAME);
        Assert.Contains(roles, r => r.Name == RoleConstants.ADMIN_ROLE_NAME);
        Assert.Contains(roles, r => r.Name == RoleConstants.USER_ROLE_NAME);
    }

    [Fact]
    public async Task GetRoleByRoleIdAsync_ReturnsNull_WhenRoleNotFound()
    {
        var result = await _roleProcessor.GetRoleAsync(Guid.Empty);
        Assert.Equal(GetRoleResultCode.RoleNotFoundError, result.ResultCode);
        Assert.Null(result.Role);
    }

    [Fact]
    public async Task GetRoleByRoleIdAsync_ReturnsRole_WhenRoleExists()
    {
        var ownerAccount = await CreateAccountAsync();
        var request = new CreateRoleRequest(VALID_ROLE_NAME, ownerAccount.Id);
        var createResult = await _roleProcessor.CreateRoleAsync(request);

        var result = await _roleProcessor.GetRoleAsync(createResult.CreatedId);

        Assert.NotNull(result.Role);
        Assert.Equal(VALID_ROLE_NAME, result.Role!.Name);
    }

    [Fact]
    public async Task GetRoleByRoleNameAsync_ReturnsNull_WhenRoleNotFound()
    {
        var result = await _roleProcessor.GetRoleAsync("nonexistent", Guid.Empty);
        Assert.Equal(GetRoleResultCode.RoleNotFoundError, result.ResultCode);
        Assert.Null(result.Role);
    }

    [Fact]
    public async Task GetRoleByRoleNameAsync_ReturnsRole_WhenRoleExists()
    {
        var ownerAccount = await CreateAccountAsync();
        var request = new CreateRoleRequest(VALID_ROLE_NAME, ownerAccount.Id);
        await _roleProcessor.CreateRoleAsync(request);

        var result = await _roleProcessor.GetRoleAsync(VALID_ROLE_NAME, ownerAccount.Id);

        Assert.NotNull(result.Role);
        Assert.Equal(VALID_ROLE_NAME, result.Role!.Name);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_Fails_WhenRoleDoesNotExist()
    {
        var request = new AssignPermissionsToRoleRequest([], Guid.CreateVersion7());
        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);
        Assert.Equal(AssignPermissionsToRoleResultCode.RoleNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_Fails_WhenPermissionsNotProvided()
    {
        var (role, _) = await CreateRoleAsync();
        var request = new AssignPermissionsToRoleRequest([], role.Id);

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
        var (role, account) = await CreateRoleAsync();
        var permission = await CreatePermissionAsync(account.Id);
        var request = new AssignPermissionsToRoleRequest([permission.Id], role.Id);

        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);

        Assert.Equal(AssignPermissionsToRoleResultCode.Success, result.ResultCode);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var roleEntity = await roleRepo.GetAsync(
            r => r.Id == role.Id,
            include: q => q.Include(r => r.Permissions)
        );

        Assert.NotNull(roleEntity);
        Assert.NotNull(roleEntity.Permissions);
        Assert.Contains(roleEntity.Permissions, p => p.Id == permission.Id);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_PartiallySucceeds_WhenSomePermissionsExistForRole()
    {
        var (role, account) = await CreateRoleAsync();
        var existingPermission = await CreatePermissionAsync(
            account.Id,
            isSystemDefined: false,
            role.Id
        );
        var newPermission = await CreatePermissionAsync(
            account.Id,
            isSystemDefined: false,
            Guid.CreateVersion7()
        );
        var request = new AssignPermissionsToRoleRequest(
            [existingPermission.Id, newPermission.Id],
            role.Id
        );

        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);

        Assert.Equal(AssignPermissionsToRoleResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some permission ids are invalid (not found, from different account, or already assigned to role)",
            result.Message
        );
        Assert.Equal(1, result.AddedPermissionCount);
        Assert.NotEmpty(result.InvalidPermissionIds);
        Assert.Contains(existingPermission.Id, result.InvalidPermissionIds);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_PartiallySucceeds_WhenSomePermissionsDoNotExist()
    {
        var (role, _) = await CreateRoleAsync();
        var permission = await CreatePermissionAsync(role.AccountId);
        var request = new AssignPermissionsToRoleRequest([Guid.Empty, permission.Id], role.Id);

        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);

        Assert.Equal(AssignPermissionsToRoleResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some permission ids are invalid (not found, from different account, or already assigned to role)",
            result.Message
        );
        Assert.NotEmpty(result.InvalidPermissionIds);
        Assert.Contains(Guid.Empty, result.InvalidPermissionIds);
        Assert.DoesNotContain(permission.Id, result.InvalidPermissionIds);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_PartiallySucceeds_WhenSomePermissionsBelongToDifferentAccount()
    {
        var (role, account) = await CreateRoleAsync();
        var permissionSameAccount = await CreatePermissionAsync(account.Id);
        var permissionDifferentAccount = await CreatePermissionAsync(Guid.CreateVersion7());
        var request = new AssignPermissionsToRoleRequest(
            [permissionSameAccount.Id, permissionDifferentAccount.Id],
            role.Id
        );

        var result = await _roleProcessor.AssignPermissionsToRoleAsync(request);

        Assert.Equal(AssignPermissionsToRoleResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some permission ids are invalid (not found, from different account, or already assigned to role)",
            result.Message
        );
        Assert.Equal(1, result.AddedPermissionCount);
        Assert.NotEmpty(result.InvalidPermissionIds);
        Assert.Contains(permissionDifferentAccount.Id, result.InvalidPermissionIds);
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_Fails_WhenAllPermissionsExistForRole()
    {
        var (role, account) = await CreateRoleAsync();
        var permissionIds = new List<Guid>
        {
            (await CreatePermissionAsync(account.Id, isSystemDefined: false, role.Id)).Id,
            (await CreatePermissionAsync(account.Id, isSystemDefined: false, role.Id)).Id,
        };
        var request = new AssignPermissionsToRoleRequest(permissionIds, role.Id);
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
        var (role, _) = await CreateRoleAsync();
        var permissionIds = _fixture.CreateMany<Guid>().ToList();
        var request = new AssignPermissionsToRoleRequest(permissionIds, role.Id);

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
        var (role, _) = await CreateRoleAsync();
        var permissionIds = new List<Guid>()
        {
            (await CreatePermissionAsync(Guid.CreateVersion7())).Id,
            (await CreatePermissionAsync(Guid.CreateVersion7())).Id,
        };
        var request = new AssignPermissionsToRoleRequest(permissionIds, role.Id);

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
        var (role, _) = await CreateRoleAsync();

        var result = await _roleProcessor.DeleteRoleAsync(role.Id);

        Assert.Equal(DeleteRoleResultCode.Success, result.ResultCode);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var roleEntity = await roleRepo.GetAsync(r => r.Id == role.Id);
        Assert.Null(roleEntity);
    }

    [Fact]
    public async Task DeleteRoleAsync_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        var result = await _roleProcessor.DeleteRoleAsync(Guid.CreateVersion7());

        Assert.Equal(DeleteRoleResultCode.RoleNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DeleteRoleAsync_ReturnsSystemDefinedRoleError_WhenRoleIsSystemDefined()
    {
        var account = await CreateAccountAsync();
        await _roleProcessor.CreateDefaultSystemRolesAsync(account.Id);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var ownerRole = await roleRepo.GetAsync(r =>
            r.AccountId == account.Id && r.Name == RoleConstants.OWNER_ROLE_NAME
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
        var account = await CreateAccountAsync();
        await _roleProcessor.CreateDefaultSystemRolesAsync(account.Id);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var systemRoles = await roleRepo.ListAsync(r =>
            r.AccountId == account.Id && r.IsSystemDefined
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
        var request = new RemovePermissionsFromRoleRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );

        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        Assert.Equal(RemovePermissionsFromRoleResultCode.RoleNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_Fails_WhenPermissionsNotAssignedToRole()
    {
        var (role, _) = await CreateRoleAsync();
        var request = new RemovePermissionsFromRoleRequest([Guid.CreateVersion7()], role.Id);

        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        Assert.Equal(
            RemovePermissionsFromRoleResultCode.InvalidPermissionIdsError,
            result.ResultCode
        );
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_Succeeds_WhenNonSystemRoleAndNonSystemPermission()
    {
        var (role, account) = await CreateRoleAsync();
        var permission = await CreatePermissionAsync(account.Id, isSystemDefined: false, role.Id);

        // Assign permission to role first
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        role!.Permissions = [permission];
        await roleRepo.UpdateAsync(role);

        var request = new RemovePermissionsFromRoleRequest([permission.Id], role.Id);
        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        Assert.Equal(RemovePermissionsFromRoleResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedPermissionCount);
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_Succeeds_WhenNonSystemRoleAndSystemPermission()
    {
        var (role, account) = await CreateRoleAsync();
        var permission = await CreatePermissionAsync(account.Id, isSystemDefined: true, role.Id);

        // Assign permission to role first
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        role!.Permissions = [permission!];
        await roleRepo.UpdateAsync(role);

        var request = new RemovePermissionsFromRoleRequest([permission.Id], role.Id);
        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        // Non-system role can remove ANY permission
        Assert.Equal(RemovePermissionsFromRoleResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedPermissionCount);
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_Succeeds_WhenSystemRoleAndNonSystemPermission()
    {
        var account = await CreateAccountAsync();
        await _roleProcessor.CreateDefaultSystemRolesAsync(account.Id);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var systemRole = await roleRepo.GetAsync(r =>
            r.AccountId == account.Id && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var permission = await CreatePermissionAsync(account.Id, isSystemDefined: false);

        // Assign non-system permission to system role
        systemRole!.Permissions = [permission];
        await roleRepo.UpdateAsync(systemRole);

        var request = new RemovePermissionsFromRoleRequest([permission.Id], systemRole.Id);
        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        // System role CAN remove non-system permissions
        Assert.Equal(RemovePermissionsFromRoleResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedPermissionCount);
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_Fails_WhenSystemRoleAndSystemPermission()
    {
        var account = await CreateAccountAsync();
        await _roleProcessor.CreateDefaultSystemRolesAsync(account.Id);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var systemRole = await roleRepo.GetAsync(r =>
            r.AccountId == account.Id && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var permission = await CreatePermissionAsync(account.Id, isSystemDefined: true);

        // Assign system permission to system role
        systemRole!.Permissions = [permission];
        await roleRepo.UpdateAsync(systemRole);

        var request = new RemovePermissionsFromRoleRequest([permission.Id], systemRole.Id);
        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        // System role CANNOT remove system permissions
        Assert.Equal(
            RemovePermissionsFromRoleResultCode.SystemPermissionRemovalError,
            result.ResultCode
        );
        Assert.Equal(0, result.RemovedPermissionCount);
        Assert.Contains(permission.Id, result.SystemPermissionIds);
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_PartialSuccess_WhenSystemRoleAndMixedPermissions()
    {
        var account = await CreateAccountAsync();
        await _roleProcessor.CreateDefaultSystemRolesAsync(account.Id);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var systemRole = await roleRepo.GetAsync(r =>
            r.AccountId == account.Id && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var nonSystemPermission = await CreatePermissionAsync(account.Id, isSystemDefined: false);
        var systemPermission = await CreatePermissionAsync(account.Id, isSystemDefined: true);

        // Assign both permissions to system role
        systemRole!.Permissions = [nonSystemPermission, systemPermission];
        await roleRepo.UpdateAsync(systemRole);

        var request = new RemovePermissionsFromRoleRequest(
            [nonSystemPermission.Id, systemPermission.Id],
            systemRole.Id
        );
        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(request);

        // Should remove non-system but not system permission
        Assert.Equal(RemovePermissionsFromRoleResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(1, result.RemovedPermissionCount);
        Assert.Contains(systemPermission.Id, result.SystemPermissionIds);
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
