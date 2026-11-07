using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.IdentityProviders.Enums;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class FederatedIdentityServiceTelemetryDecorator(
    IFederatedIdentityService inner,
    ILogger<FederatedIdentityServiceTelemetryDecorator> logger
) : IFederatedIdentityService
{
    private readonly IFederatedIdentityService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<FederatedIdentityServiceTelemetryDecorator> _logger =
        logger.ThrowIfNull(nameof(logger));

    public async Task<AuthMethodsResult> GetAuthMethodsAsync() =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedIdentityService),
            _inner.GetAuthMethodsAsync,
            logResult: true
        );

    public async Task<UnlinkFederatedIdentityResult> UnlinkFederatedIdentityAsync(
        IdentityProviderType type
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedIdentityService),
            new { type },
            () => _inner.UnlinkFederatedIdentityAsync(type),
            logResult: true
        );
}
