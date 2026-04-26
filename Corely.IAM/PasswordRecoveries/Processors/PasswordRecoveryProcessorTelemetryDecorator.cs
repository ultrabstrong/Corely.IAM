using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.PasswordRecoveries.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.PasswordRecoveries.Processors;

internal class PasswordRecoveryProcessorTelemetryDecorator(
    IPasswordRecoveryProcessor inner,
    ILogger<PasswordRecoveryProcessorTelemetryDecorator> logger
) : IPasswordRecoveryProcessor
{
    private readonly IPasswordRecoveryProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<PasswordRecoveryProcessorTelemetryDecorator> _logger =
        logger.ThrowIfNull(nameof(logger));

    public async Task<RequestPasswordRecoveryResult> RequestPasswordRecoveryAsync(
        RequestPasswordRecoveryRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(PasswordRecoveryProcessor),
            request,
            () => _inner.RequestPasswordRecoveryAsync(request),
            logResult: true
        );

    public async Task<ValidatePasswordRecoveryTokenResult> ValidatePasswordRecoveryTokenAsync(
        ValidatePasswordRecoveryTokenRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(PasswordRecoveryProcessor),
            request,
            () => _inner.ValidatePasswordRecoveryTokenAsync(request),
            logResult: true
        );

    public async Task<ResetPasswordWithRecoveryResult> ResetPasswordWithRecoveryAsync(
        ResetPasswordWithRecoveryRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(PasswordRecoveryProcessor),
            request,
            () => _inner.ResetPasswordWithRecoveryAsync(request),
            logResult: true
        );
}
