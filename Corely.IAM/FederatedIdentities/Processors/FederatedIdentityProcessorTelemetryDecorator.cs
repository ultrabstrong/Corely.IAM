using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.FederatedIdentities.Processors;

internal class FederatedIdentityProcessorTelemetryDecorator(
    IFederatedIdentityProcessor inner,
    ILogger<FederatedIdentityProcessorTelemetryDecorator> logger
) : IFederatedIdentityProcessor
{
    private readonly IFederatedIdentityProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<FederatedIdentityProcessorTelemetryDecorator> _logger =
        logger.ThrowIfNull(nameof(logger));

    public async Task<Guid?> FindUserIdAsync(string issuer, string subject) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedIdentityProcessor),
            new { issuer },
            () => _inner.FindUserIdAsync(issuer, subject),
            logResult: true
        );

    public async Task<LinkFederatedIdentityResult> LinkAsync(
        Guid userId,
        ExternalIdentity identity
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedIdentityProcessor),
            new { userId, identity.Type },
            () => _inner.LinkAsync(userId, identity),
            logResult: true
        );

    public async Task<LinkFederatedIdentityResult> LinkToNewUserAsync(
        Guid userId,
        ExternalIdentity identity
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedIdentityProcessor),
            new { userId, identity.Type },
            () => _inner.LinkToNewUserAsync(userId, identity),
            logResult: true
        );

    public async Task<UnlinkFederatedIdentityResult> UnlinkAsync(
        Guid userId,
        IdentityProviderType type
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedIdentityProcessor),
            new { userId, type },
            () => _inner.UnlinkAsync(userId, type),
            logResult: true
        );

    public async Task<IReadOnlyList<LinkedIdentity>> ListAsync(Guid userId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedIdentityProcessor),
            new { userId },
            () => _inner.ListAsync(userId),
            logResult: true
        );

    public async Task<bool> RecordReauthenticationAsync(
        Guid userId,
        ExternalIdentity identity,
        Guid sessionId
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedIdentityProcessor),
            new { userId, identity.Type },
            () => _inner.RecordReauthenticationAsync(userId, identity, sessionId),
            logResult: true
        );

    public async Task<bool> ConsumeReauthenticationAsync(Guid userId, Guid sessionId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedIdentityProcessor),
            new { userId },
            () => _inner.ConsumeReauthenticationAsync(userId, sessionId),
            logResult: true
        );

    public async Task<bool> HasAnyAsync(Guid userId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedIdentityProcessor),
            new { userId },
            () => _inner.HasAnyAsync(userId),
            logResult: true
        );

    public async Task<int> CountSoleSignInUsersAsync(IdentityProviderType type) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(FederatedIdentityProcessor),
            new { type },
            () => _inner.CountSoleSignInUsersAsync(type),
            logResult: true
        );
}
