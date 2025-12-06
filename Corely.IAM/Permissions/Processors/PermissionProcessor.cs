using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
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
                p.AccountId == permission.AccountId
                && p.ResourceType == permission.ResourceType
                && p.ResourceId == permission.ResourceId
                && p.Create == permission.Create
                && p.Read == permission.Read
                && p.Update == permission.Update
                && p.Delete == permission.Delete
                && p.Execute == permission.Execute
            )
        )
        {
            _logger.LogWarning(
                "Permission already exists for {ResourceType} - {ResourceId}",
                permission.ResourceType,
                permission.ResourceId
            );
            return new CreatePermissionResult(
                CreatePermissionResultCode.PermissionExistsError,
                $"Permission already exists for {permission.ResourceType} - {permission.ResourceId}",
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
                ResourceType = "user",
                ResourceId = 0,
                Create = true,
                Read = true,
                Update = true,
                Delete = true,
                Execute = true,
                Description = "Admin User Access",
            },
            new()
            {
                AccountId = accountId,
                ResourceType = "account",
                ResourceId = 0,
                Create = true,
                Read = true,
                Update = true,
                Delete = true,
                Execute = true,
                Description = "Admin Account Access",
            },
            new()
            {
                AccountId = accountId,
                ResourceType = "group",
                ResourceId = 0,
                Create = true,
                Read = true,
                Update = true,
                Delete = true,
                Execute = true,
                Description = "Admin Group Access",
            },
            new()
            {
                AccountId = accountId,
                ResourceType = "role",
                ResourceId = 0,
                Create = true,
                Read = true,
                Update = true,
                Delete = true,
                Execute = true,
                Description = "Admin Role Access",
            },
            new()
            {
                AccountId = accountId,
                ResourceType = "permission",
                ResourceId = 0,
                Create = true,
                Read = true,
                Update = true,
                Delete = true,
                Execute = true,
                Description = "Admin Permission Access",
            },
        ];

        await _permissionRepo.CreateAsync(permissionEntities);
    }
}
