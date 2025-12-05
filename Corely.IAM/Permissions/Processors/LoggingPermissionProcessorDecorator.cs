using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Permissions.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Permissions.Processors;

internal class LoggingPermissionProcessorDecorator : IPermissionProcessor
{
    private readonly IPermissionProcessor _inner;
    private readonly ILogger<LoggingPermissionProcessorDecorator> _logger;

    public LoggingPermissionProcessorDecorator(
        IPermissionProcessor inner,
        ILogger<LoggingPermissionProcessorDecorator> logger
    )
    {
        _inner = inner.ThrowIfNull(nameof(inner));
        _logger = logger.ThrowIfNull(nameof(logger));
    }

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
}
