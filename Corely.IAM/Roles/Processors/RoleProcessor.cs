using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Mappers;
using Corely.IAM.Roles.Models;
using Corely.IAM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Roles.Processors;

internal class RoleProcessor(
    IRepo<RoleEntity> roleRepo,
    IReadonlyRepo<AccountEntity> accountRepo,
    IReadonlyRepo<PermissionEntity> permissionRepo,
    IValidationProvider validationProvider,
    ILogger<RoleProcessor> logger
) : IRoleProcessor
{
    private readonly IRepo<RoleEntity> _roleRepo = roleRepo.ThrowIfNull(nameof(roleRepo));
    private readonly IReadonlyRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IReadonlyRepo<PermissionEntity> _permissionRepo = permissionRepo.ThrowIfNull(
        nameof(permissionRepo)
    );
    private readonly IValidationProvider _validationProvider = validationProvider.ThrowIfNull(
        nameof(validationProvider)
    );
    private readonly ILogger<RoleProcessor> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<CreateRoleResult> CreateRoleAsync(CreateRoleRequest createRoleRequest)
    {
        ArgumentNullException.ThrowIfNull(createRoleRequest, nameof(createRoleRequest));

        var role = createRoleRequest.ToRole();
        _validationProvider.ThrowIfInvalid(role);

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

        if (await _roleRepo.AnyAsync(r => r.AccountId == role.AccountId && r.Name == role.Name))
        {
            _logger.LogInformation("Role with name {RoleName} already exists", role.Name);
            return new CreateRoleResult(
                CreateRoleResultCode.RoleExistsError,
                $"Role with name {role.Name} already exists",
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

    public async Task<GetRoleResult> GetRoleAsync(int roleId)
    {
        var roleEntity = await _roleRepo.GetAsync(r => r.Id == roleId);

        if (roleEntity == null)
        {
            _logger.LogInformation("Role with Id {RoleId} not found", roleId);
            return new GetRoleResult(
                GetRoleResultCode.RoleNotFoundError,
                $"Role with Id {roleId} not found",
                null
            );
        }

        return new GetRoleResult(GetRoleResultCode.Success, string.Empty, roleEntity.ToModel());
    }

    public async Task<GetRoleResult> GetRoleAsync(string roleName, int ownerAccountId)
    {
        var roleEntity = await _roleRepo.GetAsync(r =>
            r.Name == roleName && r.AccountId == ownerAccountId
        );

        if (roleEntity == null)
        {
            _logger.LogInformation("Role with name {RoleName} not found", roleName);
            return new GetRoleResult(
                GetRoleResultCode.RoleNotFoundError,
                $"Role with name {roleName} not found",
                null
            );
        }

        return new GetRoleResult(GetRoleResultCode.Success, string.Empty, roleEntity.ToModel());
    }

    public async Task<AssignPermissionsToRoleResult> AssignPermissionsToRoleAsync(
        AssignPermissionsToRoleRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var roleEntity = await _roleRepo.GetAsync(r => r.Id == request.RoleId);
        if (roleEntity == null)
        {
            _logger.LogInformation("Role with Id {RoleId} not found", request.RoleId);
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

    public async Task<RemovePermissionsFromRoleResult> RemovePermissionsFromRoleAsync(
        RemovePermissionsFromRoleRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var roleEntity = await _roleRepo.GetAsync(
            r => r.Id == request.RoleId,
            include: q => q.Include(r => r.Permissions)
        );
        if (roleEntity == null)
        {
            _logger.LogInformation("Role with Id {RoleId} not found", request.RoleId);
            return new RemovePermissionsFromRoleResult(
                RemovePermissionsFromRoleResultCode.RoleNotFoundError,
                $"Role with Id {request.RoleId} not found",
                0,
                request.PermissionIds
            );
        }

        var permissionsToRemove =
            roleEntity.Permissions?.Where(p => request.PermissionIds.Contains(p.Id)).ToList() ?? [];

        if (permissionsToRemove.Count == 0)
        {
            _logger.LogInformation(
                "All permission ids are invalid (not found or not assigned to role) : {@InvalidPermissionIds}",
                request.PermissionIds
            );
            return new RemovePermissionsFromRoleResult(
                RemovePermissionsFromRoleResultCode.InvalidPermissionIdsError,
                "All permission ids are invalid (not found or not assigned to role)",
                0,
                request.PermissionIds
            );
        }

        // Permission removal rules:
        // 1. If role is not system-defined -> allow all permission removal
        // 2. If role is system-defined -> allow non-system permission removal
        // 3. If role is system-defined -> disallow system permission removal
        var blockedSystemPermissionIds = new List<int>();

        if (roleEntity.IsSystemDefined)
        {
            var systemPermissions = permissionsToRemove.Where(p => p.IsSystemDefined).ToList();
            if (systemPermissions.Count > 0)
            {
                blockedSystemPermissionIds = [.. systemPermissions.Select(p => p.Id)];
                _logger.LogInformation(
                    "Cannot remove system-defined permissions {@SystemPermissionIds} from system-defined role {RoleId}",
                    blockedSystemPermissionIds,
                    request.RoleId
                );

                // If ALL permissions being removed are system permissions, return error
                if (systemPermissions.Count == permissionsToRemove.Count)
                {
                    return new RemovePermissionsFromRoleResult(
                        RemovePermissionsFromRoleResultCode.SystemPermissionRemovalError,
                        "Cannot remove system-defined permissions from a system-defined role.",
                        0,
                        [],
                        blockedSystemPermissionIds
                    );
                }

                // Otherwise, remove non-system permissions only
                permissionsToRemove = [.. permissionsToRemove.Where(p => !p.IsSystemDefined)];
            }
        }

        foreach (var permission in permissionsToRemove)
        {
            roleEntity.Permissions!.Remove(permission);
        }

        if (permissionsToRemove.Count > 0)
        {
            await _roleRepo.UpdateAsync(roleEntity);
        }

        // Calculate invalid IDs (requested but not actually removed)
        var invalidPermissionIds = request
            .PermissionIds.Except(permissionsToRemove.Select(p => p.Id))
            .Except(blockedSystemPermissionIds)
            .ToList();

        // Return appropriate result based on what happened
        if (blockedSystemPermissionIds.Count > 0 || invalidPermissionIds.Count > 0)
        {
            _logger.LogInformation(
                "Some permissions were not removed: invalid {@InvalidPermissionIds}, system-defined {@SystemPermissionIds}",
                invalidPermissionIds,
                blockedSystemPermissionIds
            );
            return new RemovePermissionsFromRoleResult(
                RemovePermissionsFromRoleResultCode.PartialSuccess,
                blockedSystemPermissionIds.Count > 0
                    ? "Some permissions could not be removed (invalid or system-defined permissions on system role)"
                    : "Some permission ids are invalid (not found or not assigned to role)",
                permissionsToRemove.Count,
                invalidPermissionIds,
                blockedSystemPermissionIds
            );
        }

        return new RemovePermissionsFromRoleResult(
            RemovePermissionsFromRoleResultCode.Success,
            string.Empty,
            permissionsToRemove.Count,
            []
        );
    }

    public async Task<DeleteRoleResult> DeleteRoleAsync(int roleId)
    {
        var roleEntity = await _roleRepo.GetAsync(r => r.Id == roleId);
        if (roleEntity == null)
        {
            _logger.LogInformation("Role with Id {RoleId} not found", roleId);
            return new DeleteRoleResult(
                DeleteRoleResultCode.RoleNotFoundError,
                $"Role with Id {roleId} not found"
            );
        }

        if (roleEntity.IsSystemDefined)
        {
            _logger.LogInformation(
                "Cannot delete system-defined role {RoleName} with Id {RoleId}",
                roleEntity.Name,
                roleId
            );
            return new DeleteRoleResult(
                DeleteRoleResultCode.SystemDefinedRoleError,
                $"Cannot delete system-defined role '{roleEntity.Name}'. System-defined roles are required for account ownership and access control."
            );
        }

        await _roleRepo.DeleteAsync(roleEntity);

        _logger.LogInformation("Role with Id {RoleId} deleted", roleId);
        return new DeleteRoleResult(DeleteRoleResultCode.Success, string.Empty);
    }
}
