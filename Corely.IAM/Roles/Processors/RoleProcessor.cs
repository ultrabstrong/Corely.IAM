using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Mappers;
using Corely.IAM.Roles.Models;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Roles.Processors;

internal class RoleProcessor : IRoleProcessor
{
    private readonly IRepo<RoleEntity> _roleRepo;
    private readonly IReadonlyRepo<AccountEntity> _accountRepo;
    private readonly IReadonlyRepo<PermissionEntity> _permissionRepo;
    private readonly IValidationProvider _validationProvider;
    private readonly ILogger<RoleProcessor> _logger;

    public RoleProcessor(
        IRepo<RoleEntity> roleRepo,
        IReadonlyRepo<AccountEntity> accountRepo,
        IReadonlyRepo<PermissionEntity> permissionRepo,
        IValidationProvider validationProvider,
        ILogger<RoleProcessor> logger
    )
    {
        _roleRepo = roleRepo.ThrowIfNull(nameof(roleRepo));
        _accountRepo = accountRepo.ThrowIfNull(nameof(accountRepo));
        _permissionRepo = permissionRepo.ThrowIfNull(nameof(permissionRepo));
        _validationProvider = validationProvider.ThrowIfNull(nameof(validationProvider));
        _logger = logger.ThrowIfNull(nameof(logger));
    }

    public async Task<CreateRoleResult> CreateRoleAsync(CreateRoleRequest createRoleRequest)
    {
        ArgumentNullException.ThrowIfNull(createRoleRequest, nameof(createRoleRequest));

        var role = createRoleRequest.ToRole();
        _validationProvider.ThrowIfInvalid(role);

        if (await _roleRepo.AnyAsync(r => r.AccountId == role.AccountId && r.Name == role.Name))
        {
            _logger.LogWarning("Role with name {RoleName} already exists", role.Name);
            return new CreateRoleResult(
                CreateRoleResultCode.RoleExistsError,
                $"Role with name {role.Name} already exists",
                -1
            );
        }

        var accountEntity = await _accountRepo.GetAsync(a => a.Id == role.AccountId);
        if (accountEntity == null)
        {
            _logger.LogWarning("Account with Id {AccountId} not found", role.AccountId);
            return new CreateRoleResult(
                CreateRoleResultCode.AccountNotFoundError,
                $"Account with Id {role.AccountId} not found",
                -1
            );
        }

        var roleEntity = role.ToEntity();
        var created = await _roleRepo.CreateAsync(roleEntity);

        return new CreateRoleResult(CreateRoleResultCode.Success, string.Empty, created.Id);
    }

    public async Task<CreateDefaultSystemRolesResult> CreateDefaultSystemRolesAsync(
        int ownerAccountId
    )
    {
        var ownerRole = new RoleEntity
        {
            AccountId = ownerAccountId,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
        };
        var adminRole = new RoleEntity
        {
            AccountId = ownerAccountId,
            Name = RoleConstants.ADMIN_ROLE_NAME,
            IsSystemDefined = true,
        };
        var userRole = new RoleEntity
        {
            AccountId = ownerAccountId,
            Name = RoleConstants.USER_ROLE_NAME,
            IsSystemDefined = true,
        };

        await _roleRepo.CreateAsync([ownerRole, adminRole, userRole]);

        return new CreateDefaultSystemRolesResult(ownerRole.Id, adminRole.Id, userRole.Id);
    }

    public async Task<Role?> GetRoleAsync(int roleId)
    {
        var roleEntity = await _roleRepo.GetAsync(r => r.Id == roleId);

        if (roleEntity == null)
        {
            _logger.LogInformation("Role with Id {RoleId} not found", roleId);
            return null;
        }

        return roleEntity.ToModel();
    }

    public async Task<Role?> GetRoleAsync(string roleName, int ownerAccountId)
    {
        var roleEntity = await _roleRepo.GetAsync(r =>
            r.Name == roleName && r.AccountId == ownerAccountId
        );

        if (roleEntity == null)
        {
            _logger.LogInformation("Role with name {RoleName} not found", roleName);
            return null;
        }

        return roleEntity.ToModel();
    }

    public async Task<AssignPermissionsToRoleResult> AssignPermissionsToRoleAsync(
        AssignPermissionsToRoleRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var roleEntity = await _roleRepo.GetAsync(r => r.Id == request.RoleId);
        if (roleEntity == null)
        {
            _logger.LogWarning("Role with Id {RoleId} not found", request.RoleId);
            return new AssignPermissionsToRoleResult(
                AssignPermissionsToRoleResultCode.RoleNotFoundError,
                $"Role with Id {request.RoleId} not found",
                0,
                request.PermissionIds
            );
        }

        var permissionEntities = await _permissionRepo.ListAsync(p =>
            request.PermissionIds.Contains(p.Id)
            && !p.Roles!.Any(r => r.Id == roleEntity.Id)
            && p.Account!.Id == roleEntity.AccountId
        );

        if (permissionEntities.Count == 0)
        {
            _logger.LogInformation(
                "All permission ids are invalid (not found, from different account, or already assigned to role) : {@InvalidPermissionIds}",
                request.PermissionIds
            );
            return new AssignPermissionsToRoleResult(
                AssignPermissionsToRoleResultCode.InvalidPermissionIdsError,
                "All permission ids are invalid (not found, from different account, or already assigned to role)",
                0,
                request.PermissionIds
            );
        }

        roleEntity.Permissions ??= [];
        foreach (var permission in permissionEntities)
        {
            roleEntity.Permissions.Add(permission);
        }

        await _roleRepo.UpdateAsync(roleEntity);

        var invalidPermissionIds = request
            .PermissionIds.Except(permissionEntities.Select(p => p.Id))
            .ToList();
        if (invalidPermissionIds.Count > 0)
        {
            _logger.LogInformation(
                "Some permission ids are invalid (not found, from different account, or already assigned to role) : {@InvalidPermissionIds}",
                invalidPermissionIds
            );
            return new AssignPermissionsToRoleResult(
                AssignPermissionsToRoleResultCode.PartialSuccess,
                "Some permission ids are invalid (not found, from different account, or already assigned to role)",
                permissionEntities.Count,
                invalidPermissionIds
            );
        }

        return new AssignPermissionsToRoleResult(
            AssignPermissionsToRoleResultCode.Success,
            string.Empty,
            permissionEntities.Count,
            invalidPermissionIds
        );
    }

    public async Task<DeleteRoleResult> DeleteRoleAsync(int roleId)
    {
        var roleEntity = await _roleRepo.GetAsync(r => r.Id == roleId);
        if (roleEntity == null)
        {
            _logger.LogWarning("Role with Id {RoleId} not found", roleId);
            return new DeleteRoleResult(
                DeleteRoleResultCode.RoleNotFoundError,
                $"Role with Id {roleId} not found"
            );
        }

        await _roleRepo.DeleteAsync(roleEntity);

        _logger.LogInformation("Role with Id {RoleId} deleted", roleId);
        return new DeleteRoleResult(DeleteRoleResultCode.Success, string.Empty);
    }
}
