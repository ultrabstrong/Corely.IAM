using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Permissions.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Permissions.Processors;

internal class PermissionProcessorLoggingDecorator(
    IPermissionProcessor inner,
    ILogger<PermissionProcessorLoggingDecorator> logger
) : IPermissionProcessor
{
    private readonly IPermissionProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<PermissionProcessorLoggingDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<CreatePermissionResult> CreatePermissionAsync(
        CreatePermissionRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(PermissionProcessor),
            request,
            () => _inner.CreatePermissionAsync(request),
            logResult: true
        );

    public async Task CreateDefaultSystemPermissionsAsync(int accountId) =>
        await _logger.ExecuteWithLogging(
            nameof(PermissionProcessor),
            accountId,
            () => _inner.CreateDefaultSystemPermissionsAsync(accountId)
        );

    public async Task<DeletePermissionResult> DeletePermissionAsync(int permissionId) =>
        await _logger.ExecuteWithLogging(
            nameof(PermissionProcessor),
            permissionId,
            () => _inner.DeletePermissionAsync(permissionId),
            logResult: true
        );
}
