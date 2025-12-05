using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Mappers;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Permissions.Processors;

internal class PermissionProcessor : IPermissionProcessor
{
    private readonly IRepo<PermissionEntity> _permissionRepo;
    private readonly IReadonlyRepo<AccountEntity> _accountRepo;
    private readonly IValidationProvider _validationProvider;
    private readonly ILogger<PermissionProcessor> _logger;

    public PermissionProcessor(
        IRepo<PermissionEntity> permissionRepo,
        IReadonlyRepo<AccountEntity> accountRepo,
        IValidationProvider validationProvider,
        ILogger<PermissionProcessor> logger
    )
    {
        _permissionRepo = permissionRepo.ThrowIfNull(nameof(permissionRepo));
        _accountRepo = accountRepo.ThrowIfNull(nameof(accountRepo));
        _validationProvider = validationProvider.ThrowIfNull(nameof(validationProvider));
        _logger = logger.ThrowIfNull(nameof(logger));
    }

    public async Task<CreatePermissionResult> CreatePermissionAsync(CreatePermissionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var permission = request.ToPermission();
        _validationProvider.ThrowIfInvalid(permission);

        if (
            await _permissionRepo.AnyAsync(p =>
                p.AccountId == permission.AccountId && p.Name == permission.Name
            )
        )
        {
            _logger.LogWarning(
                "Permission with name {PermissionName} already exists",
                permission.Name
            );
            return new CreatePermissionResult(
                CreatePermissionResultCode.PermissionExistsError,
                $"Permission with name {permission.Name} already exists",
                -1
            );
        }

        var accountEntity = await _accountRepo.GetAsync(a => a.Id == permission.AccountId);
        if (accountEntity == null)
        {
            _logger.LogWarning("Account with Id {AccountId} not found", permission.AccountId);
            return new CreatePermissionResult(
                CreatePermissionResultCode.AccountNotFoundError,
                $"Account with Id {permission.AccountId} not found",
                -1
            );
        }

        var permissionEntity = permission.ToEntity();
        var created = await _permissionRepo.CreateAsync(permissionEntity);

        return new CreatePermissionResult(
            CreatePermissionResultCode.Success,
            "Permission created successfully",
            created.Id
        );
    }

    public async Task CreateDefaultSystemPermissionsAsync(int accountId)
    {
        PermissionEntity[] permissionEntities =
        [
            new()
            {
                AccountId = accountId,
                Name = PermissionConstants.ADMIN_USER_ACCESS_PERMISSION_NAME,
            },
            new()
            {
                AccountId = accountId,
                Name = PermissionConstants.ADMIN_ACCOUNT_ACCESS_PERMISSION_NAME,
            },
            new()
            {
                AccountId = accountId,
                Name = PermissionConstants.ADMIN_GROUP_ACCESS_PERMISSION_NAME,
            },
            new()
            {
                AccountId = accountId,
                Name = PermissionConstants.ADMIN_ROLE_ACCESS_PERMISSION_NAME,
            },
            new()
            {
                AccountId = accountId,
                Name = PermissionConstants.ADMIN_PERMISSION_ACCESS_PERMISSION_NAME,
            },
        ];

        await _permissionRepo.CreateAsync(permissionEntities);
    }
}
