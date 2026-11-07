using Corely.Common.Extensions;
using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.FederatedIdentities.Processors;

internal class FederatedIdentityProcessorAuthorizationDecorator(
    IFederatedIdentityProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IFederatedIdentityProcessor
{
    private const string UNAUTHORIZED = "Unauthorized";

    private readonly IFederatedIdentityProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public Task<Guid?> FindUserIdAsync(string issuer, string subject) =>
        _inner.FindUserIdAsync(issuer, subject);

    public async Task<LinkFederatedIdentityResult> LinkAsync(
        Guid userId,
        ExternalIdentity identity
    ) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
            ? await _inner.LinkAsync(userId, identity)
            : new(LinkFederatedIdentityResultCode.UnauthorizedError, UNAUTHORIZED);

    public Task<LinkFederatedIdentityResult> LinkToNewUserAsync(
        Guid userId,
        ExternalIdentity identity
    ) => _inner.LinkToNewUserAsync(userId, identity);

    public async Task<UnlinkFederatedIdentityResult> UnlinkAsync(
        Guid userId,
        IdentityProviderType type
    ) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
            ? await _inner.UnlinkAsync(userId, type)
            : new(UnlinkFederatedIdentityResultCode.UnauthorizedError, UNAUTHORIZED);

    public async Task<IReadOnlyList<LinkedIdentity>> ListAsync(Guid userId) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId) ? await _inner.ListAsync(userId) : [];

    public async Task<bool> RecordReauthenticationAsync(
        Guid userId,
        ExternalIdentity identity,
        Guid sessionId
    ) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
        && await _inner.RecordReauthenticationAsync(userId, identity, sessionId);

    public async Task<bool> ConsumeReauthenticationAsync(Guid userId, Guid sessionId) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
        && await _inner.ConsumeReauthenticationAsync(userId, sessionId);

    public Task<bool> HasAnyAsync(Guid userId) => _inner.HasAnyAsync(userId);

    public Task<int> CountSoleSignInUsersAsync(IdentityProviderType type) =>
        _inner.CountSoleSignInUsersAsync(type);
}
