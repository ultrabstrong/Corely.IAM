using Corely.Common.Extensions;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Extensions;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Permissions.Processors;

internal class PermissionProcessorTelemetryDecorator(
    IPermissionProcessor inner,
    ILogger<PermissionProcessorTelemetryDecorator> logger
) : IPermissionProcessor
{
    private readonly IPermissionProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<PermissionProcessorTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<CreatePermissionResult> CreatePermissionAsync(
        CreatePermissionRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(PermissionProcessor),
            request,
            () => _inner.CreatePermissionAsync(request),
            logResult: true
        );

    public async Task CreateDefaultSystemPermissionsAsync(Guid accountId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(PermissionProcessor),
            accountId,
            () => _inner.CreateDefaultSystemPermissionsAsync(accountId)
        );

    public async Task<ListResult<Permission>> ListPermissionsAsync(
        FilterBuilder<Permission>? filter,
        OrderBuilder<Permission>? order,
        int skip,
        int take
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(PermissionProcessor),
            new { skip, take },
            () => _inner.ListPermissionsAsync(filter, order, skip, take),
            logResult: true
        );

    public async Task<GetResult<Permission>> GetPermissionByIdAsync(
        Guid permissionId,
        bool hydrate
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(PermissionProcessor),
            new { permissionId, hydrate },
            () => _inner.GetPermissionByIdAsync(permissionId, hydrate),
            logResult: true
        );

    public async Task<DeletePermissionResult> DeletePermissionAsync(Guid permissionId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(PermissionProcessor),
            permissionId,
            () => _inner.DeletePermissionAsync(permissionId),
            logResult: true
        );

    public async Task<List<EffectivePermission>> GetEffectivePermissionsForUserAsync(
        string resourceType,
        Guid resourceId,
        Guid userId,
        Guid accountId
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(PermissionProcessor),
            new
            {
                resourceType,
                resourceId,
                userId,
                accountId,
            },
            () =>
                _inner.GetEffectivePermissionsForUserAsync(
                    resourceType,
                    resourceId,
                    userId,
                    accountId
                ),
            logResult: true
        );
}
