using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Processors;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Mappers;
using Corely.IAM.Roles.Models;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Roles.Processors;

internal class RoleProcessor : ProcessorBase, IRoleProcessor
{
    private readonly IRepo<RoleEntity> _roleRepo;
    private readonly IReadonlyRepo<AccountEntity> _accountRepo;
    private readonly IReadonlyRepo<PermissionEntity> _permissionRepo;

    public RoleProcessor(
        IRepo<RoleEntity> roleRepo,
        IReadonlyRepo<AccountEntity> accountRepo,
        IReadonlyRepo<PermissionEntity> permissionRepo,
        IValidationProvider validationProvider,
        ILogger<RoleProcessor> logger
    )
        : base(validationProvider, logger)
    {
        _roleRepo = roleRepo.ThrowIfNull(nameof(roleRepo));
        _accountRepo = accountRepo.ThrowIfNull(nameof(accountRepo));
        _permissionRepo = permissionRepo.ThrowIfNull(nameof(permissionRepo));
    }

    public async Task<CreateRoleResult> CreateRoleAsync(CreateRoleRequest createRoleRequest)
    {
        return await LogRequestResultAspect(
            nameof(RoleProcessor),
            nameof(CreateRoleAsync),
            createRoleRequest,
            async () =>
            {
                var role = Validate(createRoleRequest.ToRole());

                if (
                    await _roleRepo.AnyAsync(r =>
                        r.AccountId == role.AccountId && r.Name == role.Name
                    )
                )
                {
                    Logger.LogWarning("Role with name {RoleName} already exists", role.Name);
                    return new CreateRoleResult(
                        CreateRoleResultCode.RoleExistsError,
                        $"Role with name {role.Name} already exists",
                        -1
                    );
                }

                var accountEntity = await _accountRepo.GetAsync(a => a.Id == role.AccountId);
                if (accountEntity == null)
                {
                    Logger.LogWarning("Account with Id {AccountId} not found", role.AccountId);
                    return new CreateRoleResult(
                        CreateRoleResultCode.AccountNotFoundError,
                        $"Account with Id {role.AccountId} not found",
                        -1
                    );
                }

                var roleEntity = role.ToEntity(); // role is validated
                var created = await _roleRepo.CreateAsync(roleEntity);

                return new CreateRoleResult(CreateRoleResultCode.Success, string.Empty, created.Id);
            }
        );
    }

    public async Task CreateDefaultSystemRolesAsync(int ownerAccountId)
    {
        RoleEntity[] roleEntities =
        [
            new()
            {
                AccountId = ownerAccountId,
                Name = RoleConstants.OWNER_ROLE_NAME,
                IsSystemDefined = true,
            },
            new()
            {
                AccountId = ownerAccountId,
                Name = RoleConstants.ADMIN_ROLE_NAME,
                IsSystemDefined = true,
            },
            new()
            {
                AccountId = ownerAccountId,
                Name = RoleConstants.USER_ROLE_NAME,
                IsSystemDefined = true,
            },
        ];

        await _roleRepo.CreateAsync(roleEntities);
    }

    public async Task<Role?> GetRoleAsync(int roleId)
    {
        return await LogRequestAspect(
            nameof(RoleProcessor),
            nameof(GetRoleAsync),
            roleId,
            async () =>
            {
                var roleEntity = await _roleRepo.GetAsync(r => r.Id == roleId);

                if (roleEntity == null)
                {
                    Logger.LogInformation("Role with Id {RoleId} not found", roleId);
                    return null;
                }

                var role = roleEntity.ToModel();
                return role;
            }
        );
    }

    public Task<Role?> GetRoleAsync(string roleName, int ownerAccountId)
    {
        return LogRequestAspect(
            nameof(RoleProcessor),
            nameof(GetRoleAsync),
            roleName,
            async () =>
            {
                var roleEntity = await _roleRepo.GetAsync(r =>
                    r.Name == roleName && r.AccountId == ownerAccountId
                );

                if (roleEntity == null)
                {
                    Logger.LogInformation("Role with name {RoleName} not found", roleName);
                    return null;
                }

                var role = roleEntity.ToModel();
                return role;
            }
        );
    }

    public async Task<AssignPermissionsToRoleResult> AssignPermissionsToRoleAsync(
        AssignPermissionsToRoleRequest request
    )
    {
        return await LogRequestResultAspect(
            nameof(RoleProcessor),
            nameof(AssignPermissionsToRoleAsync),
            request,
            async () =>
            {
                var roleEntity = await _roleRepo.GetAsync(r => r.Id == request.RoleId);
                if (roleEntity == null)
                {
                    Logger.LogWarning("Role with Id {RoleId} not found", request.RoleId);
                    return new AssignPermissionsToRoleResult(
                        AssignPermissionsToRoleResultCode.RoleNotFoundError,
                        $"Role with Id {request.RoleId} not found",
                        0,
                        request.PermissionIds
                    );
                }

                var permissionEntities = await _permissionRepo.ListAsync(p =>
                    request.PermissionIds.Contains(p.Id) // permission exists
                    && !p.Roles!.Any(r => r.Id == roleEntity.Id) // permission not already assigned to role
                    && p.Account!.Id == roleEntity.AccountId
                ); // permission belongs to the same account as role

                if (permissionEntities.Count == 0)
                {
                    Logger.LogInformation(
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
                    Logger.LogInformation(
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
        );
    }
}
