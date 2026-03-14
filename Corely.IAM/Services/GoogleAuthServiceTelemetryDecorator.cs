using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.GoogleAuths.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class GoogleAuthServiceTelemetryDecorator(
    IGoogleAuthService inner,
    ILogger<GoogleAuthServiceTelemetryDecorator> logger
) : IGoogleAuthService
{
    private readonly IGoogleAuthService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<GoogleAuthServiceTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<LinkGoogleAuthResult> LinkGoogleAuthAsync(LinkGoogleAuthRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GoogleAuthService),
            request,
            () => _inner.LinkGoogleAuthAsync(request),
            logResult: true
        );

    public async Task<UnlinkGoogleAuthResult> UnlinkGoogleAuthAsync() =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GoogleAuthService),
            _inner.UnlinkGoogleAuthAsync,
            logResult: true
        );

    public async Task<AuthMethodsResult> GetAuthMethodsAsync() =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GoogleAuthService),
            _inner.GetAuthMethodsAsync,
            logResult: true
        );
}
