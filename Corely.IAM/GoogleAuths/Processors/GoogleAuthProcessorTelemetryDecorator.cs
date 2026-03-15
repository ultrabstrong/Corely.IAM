using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.GoogleAuths.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.GoogleAuths.Processors;

internal class GoogleAuthProcessorTelemetryDecorator(
    IGoogleAuthProcessor inner,
    ILogger<GoogleAuthProcessorTelemetryDecorator> logger
) : IGoogleAuthProcessor
{
    private readonly IGoogleAuthProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<GoogleAuthProcessorTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<LinkGoogleAuthResult> LinkGoogleAuthAsync(
        Guid userId,
        string googleIdToken
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GoogleAuthProcessor),
            new { userId },
            () => _inner.LinkGoogleAuthAsync(userId, googleIdToken),
            logResult: true
        );

    public async Task<UnlinkGoogleAuthResult> UnlinkGoogleAuthAsync(Guid userId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GoogleAuthProcessor),
            new { userId },
            () => _inner.UnlinkGoogleAuthAsync(userId),
            logResult: true
        );

    public async Task<AuthMethodsResult> GetAuthMethodsAsync(Guid userId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GoogleAuthProcessor),
            new { userId },
            () => _inner.GetAuthMethodsAsync(userId),
            logResult: true
        );

    public async Task<Guid?> GetUserIdByGoogleSubjectAsync(string googleSubjectId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GoogleAuthProcessor),
            new { googleSubjectId },
            () => _inner.GetUserIdByGoogleSubjectAsync(googleSubjectId),
            logResult: true
        );
}
