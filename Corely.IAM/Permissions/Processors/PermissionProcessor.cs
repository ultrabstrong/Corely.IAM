using System.Linq.Expressions;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Ordering;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Mappers;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Permissions.Processors;

internal class PermissionProcessor(
    IRepo<PermissionEntity> permissionRepo,
    IRepo<RoleEntity> roleRepo,
    IReadonlyRepo<AccountEntity> accountRepo,
    IValidationProvider validationProvider,
    IUserContextProvider userContextProvider,
    ILogger<PermissionProcessor> logger
) : IPermissionProcessor
{
    private readonly IRepo<PermissionEntity> _permissionRepo = permissionRepo.ThrowIfNull(
        nameof(permissionRepo)
    );
    private readonly IRepo<RoleEntity> _roleRepo = roleRepo.ThrowIfNull(nameof(roleRepo));
    private readonly IReadonlyRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IValidationProvider _validationProvider = validationProvider.ThrowIfNull(
        nameof(validationProvider)
    );
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly ILogger<PermissionProcessor> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<CreatePermissionResult> CreatePermissionAsync(CreatePermissionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var permission = request.ToPermission();
        _validationProvider.ThrowIfInvalid(permission);

        var accountEntity = await _accountRepo.GetAsync(a => a.Id == permission.AccountId);
        if (accountEntity == null)
        {
            _logger.LogWarning("Account with Id {AccountId} not found", permission.AccountId);
            return new CreatePermissionResult(
                CreatePermissionResultCode.AccountNotFoundError,
                $"Account with Id {permission.AccountId} not found",
                Guid.Empty
            );
        }

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
            _logger.LogInformation(
                "Permission already exists for {ResourceType} - {ResourceId}",
                permission.ResourceType,
                permission.ResourceId
            );
            return new CreatePermissionResult(
                CreatePermissionResultCode.PermissionExistsError,
                $"Permission already exists for {permission.ResourceType} - {permission.ResourceId}",
                Guid.Empty
            );
        }

        var permissionEntity = permission.ToEntity();
        permissionEntity.Id = Guid.CreateVersion7();
        var created = await _permissionRepo.CreateAsync(permissionEntity);

        return new CreatePermissionResult(
            CreatePermissionResultCode.Success,
            "Permission created successfully",
            created.Id
        );
    }

    public async Task CreateDefaultSystemPermissionsAsync(Guid accountId)
    {
        var ownerRole = await _roleRepo.GetAsync(r =>
            r.AccountId == accountId && r.Name == RoleConstants.OWNER_ROLE_NAME
        );
        var adminRole = await _roleRepo.GetAsync(r =>
            r.AccountId == accountId && r.Name == RoleConstants.ADMIN_ROLE_NAME
        );
        var userRole = await _roleRepo.GetAsync(r =>
            r.AccountId == accountId && r.Name == RoleConstants.USER_ROLE_NAME
        );

        PermissionEntity[] permissionEntities =
        [
            // Owner: CRUDX on all resources
            new()
            {
                Id = Guid.CreateVersion7(),
                AccountId = accountId,
                ResourceType = PermissionConstants.ALL_RESOURCE_TYPES,
                ResourceId = Guid.Empty,
                Create = true,
                Read = true,
                Update = true,
                Delete = true,
                Execute = true,
                Description = "Owner - Full access to all resources",
                IsSystemDefined = true,
                Roles = ownerRole != null ? [ownerRole] : [],
            },
            // Admin: CRUdX on all resources (no Delete)
            new()
            {
                Id = Guid.CreateVersion7(),
                AccountId = accountId,
                ResourceType = PermissionConstants.ALL_RESOURCE_TYPES,
                ResourceId = Guid.Empty,
                Create = true,
                Read = true,
                Update = true,
                Delete = false,
                Execute = true,
                Description = "Admin - Manage all resources (no delete)",
                IsSystemDefined = true,
                Roles = adminRole != null ? [adminRole] : [],
            },
            // User: cRudx on all resources (Read only)
            new()
            {
                Id = Guid.CreateVersion7(),
                AccountId = accountId,
                ResourceType = PermissionConstants.ALL_RESOURCE_TYPES,
                ResourceId = Guid.Empty,
                Create = false,
                Read = true,
                Update = false,
                Delete = false,
                Execute = false,
                Description = "User - Read-only access to all resources",
                IsSystemDefined = true,
                Roles = userRole != null ? [userRole] : [],
            },
        ];

        await _permissionRepo.CreateAsync(permissionEntities);
    }

    public async Task<ListResult<Permission>> ListPermissionsAsync(
        FilterBuilder<Permission>? filter,
        OrderBuilder<Permission>? order,
        int skip,
        int take
    )
    {
        var accountId = _userContextProvider.GetUserContext()!.CurrentAccount!.Id;

        Expression<Func<PermissionEntity, bool>> accountScope = p => p.AccountId == accountId;

        var filterPredicate = filter?.Build();
        Expression<Func<PermissionEntity, bool>> predicate;
        if (filterPredicate != null)
        {
            var mappedFilter = ExpressionMapper.MapPredicate<Permission, PermissionEntity>(
                filterPredicate
            );
            var param = Expression.Parameter(typeof(PermissionEntity), "p");
            var combined = Expression.AndAlso(
                Expression.Invoke(accountScope, param),
                Expression.Invoke(mappedFilter, param)
            );
            predicate = Expression.Lambda<Func<PermissionEntity, bool>>(combined, param);
        }
        else
        {
            predicate = accountScope;
        }

        var allEntities = await _permissionRepo.ListAsync(predicate);
        var totalCount = allEntities.Count;

        IEnumerable<PermissionEntity> ordered;
        if (order != null)
        {
            ordered = ExpressionMapper
                .ApplyOrder<Permission, PermissionEntity>(allEntities.AsQueryable(), order)
                .AsEnumerable();
        }
        else
        {
            ordered = allEntities.OrderBy(p => p.Id);
        }

        var items = ordered.Skip(skip).Take(take).Select(e => e.ToModel()).ToList();

        return new ListResult<Permission>(
            RetrieveResultCode.Success,
            string.Empty,
            PagedResult<Permission>.Create(items, totalCount, skip, take)
        );
    }

    public async Task<GetResult<Permission>> GetPermissionByIdAsync(Guid permissionId, bool hydrate)
    {
        var permissionEntity = hydrate
            ? await _permissionRepo.GetAsync(
                p => p.Id == permissionId,
                include: q => q.Include(p => p.Roles)
            )
            : await _permissionRepo.GetAsync(p => p.Id == permissionId);

        if (permissionEntity == null)
        {
            _logger.LogInformation("Permission with Id {PermissionId} not found", permissionId);
            return new GetResult<Permission>(
                RetrieveResultCode.NotFoundError,
                $"Permission with Id {permissionId} not found",
                null
            );
        }

        var permission = permissionEntity.ToModel();

        if (hydrate && permissionEntity.Roles != null)
        {
            permission.Roles = permissionEntity
                .Roles.Select(r => new ChildRef(r.Id, r.Name))
                .ToList();
        }

        return new GetResult<Permission>(RetrieveResultCode.Success, string.Empty, permission);
    }

    public async Task<DeletePermissionResult> DeletePermissionAsync(Guid permissionId)
    {
        var permissionEntity = await _permissionRepo.GetAsync(
            p => p.Id == permissionId,
            include: q => q.Include(p => p.Roles)
        );
        if (permissionEntity == null)
        {
            _logger.LogInformation("Permission with Id {PermissionId} not found", permissionId);
            return new DeletePermissionResult(
                DeletePermissionResultCode.PermissionNotFoundError,
                $"Permission with Id {permissionId} not found"
            );
        }

        if (permissionEntity.IsSystemDefined)
        {
            _logger.LogInformation(
                "Cannot delete system-defined permission with Id {PermissionId}",
                permissionId
            );
            return new DeletePermissionResult(
                DeletePermissionResultCode.SystemDefinedPermissionError,
                $"Cannot delete system-defined permission. System-defined permissions are required for account ownership and access control."
            );
        }

        // Clear join table (NoAction side - must do manually for SQL Server compatibility)
        permissionEntity.Roles?.Clear();

        await _permissionRepo.DeleteAsync(permissionEntity);

        _logger.LogInformation("Permission with Id {PermissionId} deleted", permissionId);
        return new DeletePermissionResult(DeletePermissionResultCode.Success, string.Empty);
    }
}
