using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.TotpAuths.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class MfaServiceTelemetryDecorator(
    IMfaService inner,
    ILogger<MfaServiceTelemetryDecorator> logger
) : IMfaService
{
    private readonly IMfaService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<MfaServiceTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<EnableTotpResult> EnableTotpAsync() =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(MfaService),
            _inner.EnableTotpAsync,
            logResult: true
        );

    public async Task<ConfirmTotpResult> ConfirmTotpAsync(ConfirmTotpRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(MfaService),
            request,
            () => _inner.ConfirmTotpAsync(request),
            logResult: true
        );

    public async Task<DisableTotpResult> DisableTotpAsync(DisableTotpRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(MfaService),
            request,
            () => _inner.DisableTotpAsync(request),
            logResult: true
        );

    public async Task<RegenerateTotpRecoveryCodesResult> RegenerateTotpRecoveryCodesAsync() =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(MfaService),
            _inner.RegenerateTotpRecoveryCodesAsync,
            logResult: true
        );

    public async Task<TotpStatusResult> GetTotpStatusAsync() =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(MfaService),
            _inner.GetTotpStatusAsync,
            logResult: true
        );
}
