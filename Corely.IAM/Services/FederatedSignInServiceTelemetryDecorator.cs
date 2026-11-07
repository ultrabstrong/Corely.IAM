using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.IdentityProviders.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class FederatedSignInServiceTelemetryDecorator(
    IFederatedSignInService inner,
    ILogger<FederatedSignInServiceTelemetryDecorator> logger
) : IFederatedSignInService
{
    private readonly IFederatedSignInService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<FederatedSignInServiceTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<IReadOnlyList<IdentityProviderOption>> ListIdentityProvidersAsync() =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedSignInService),
            _inner.ListIdentityProvidersAsync,
            logResult: true
        );

    public async Task<StartFederatedSignInResult> StartFederatedSignInAsync(
        StartFederatedSignInRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedSignInService),
            new { request.Type, request.Purpose },
            () => _inner.StartFederatedSignInAsync(request)
        );

    public async Task<CompleteFederatedSignInResult> CompleteFederatedSignInAsync(
        CompleteFederatedSignInRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedSignInService),
            new { request.Type, request.Purpose },
            () => _inner.CompleteFederatedSignInAsync(request)
        );
}
