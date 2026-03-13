using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.TotpAuths.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.TotpAuths.Processors;

internal class TotpAuthProcessorTelemetryDecorator(
    ITotpAuthProcessor inner,
    ILogger<TotpAuthProcessorTelemetryDecorator> logger
) : ITotpAuthProcessor
{
    private readonly ITotpAuthProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<TotpAuthProcessorTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<EnableTotpResult> EnableTotpAsync(
        Guid userId,
        string issuer,
        string userLabel
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(TotpAuthProcessor),
            new
            {
                userId,
                issuer,
                userLabel,
            },
            () => _inner.EnableTotpAsync(userId, issuer, userLabel),
            logResult: true
        );

    public async Task<ConfirmTotpResult> ConfirmTotpAsync(Guid userId, string code) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(TotpAuthProcessor),
            new { userId },
            () => _inner.ConfirmTotpAsync(userId, code),
            logResult: true
        );

    public async Task<DisableTotpResult> DisableTotpAsync(Guid userId, string code) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(TotpAuthProcessor),
            new { userId },
            () => _inner.DisableTotpAsync(userId, code),
            logResult: true
        );

    public async Task<TotpStatusResult> GetTotpStatusAsync(Guid userId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(TotpAuthProcessor),
            new { userId },
            () => _inner.GetTotpStatusAsync(userId),
            logResult: true
        );

    public async Task<RegenerateTotpRecoveryCodesResult> RegenerateTotpRecoveryCodesAsync(
        Guid userId
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(TotpAuthProcessor),
            new { userId },
            () => _inner.RegenerateTotpRecoveryCodesAsync(userId),
            logResult: true
        );

    public async Task<VerifyTotpOrRecoveryCodeResult> VerifyTotpOrRecoveryCodeAsync(
        VerifyTotpOrRecoveryCodeRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(TotpAuthProcessor),
            request,
            () => _inner.VerifyTotpOrRecoveryCodeAsync(request),
            logResult: true
        );

    public async Task<bool> IsTotpEnabledAsync(Guid userId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(TotpAuthProcessor),
            new { userId },
            () => _inner.IsTotpEnabledAsync(userId),
            logResult: true
        );
}
