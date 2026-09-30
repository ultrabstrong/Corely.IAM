using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Security.Processors;

internal class KeyRotationProcessorTelemetryDecorator(
    IKeyRotationProcessor inner,
    ILogger<KeyRotationProcessorTelemetryDecorator> logger
) : IKeyRotationProcessor
{
    private readonly IKeyRotationProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<KeyRotationProcessorTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<ModifyResult> RotateAccountKeyAsync(RotateAccountKeyRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(KeyRotationProcessor),
            request,
            () => _inner.RotateAccountKeyAsync(request),
            logResult: true
        );

    public async Task<ModifyResult> RotateCurrentUserKeyAsync(KeyType keyType) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(KeyRotationProcessor),
            keyType,
            () => _inner.RotateCurrentUserKeyAsync(keyType),
            logResult: true
        );
}
