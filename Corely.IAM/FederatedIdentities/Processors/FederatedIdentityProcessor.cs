using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.FederatedIdentities.Entities;
using Corely.IAM.FederatedIdentities.Mappers;
using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.IdentityProviders.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.FederatedIdentities.Processors;

internal class FederatedIdentityProcessor(
    IRepo<FederatedIdentityEntity> federatedIdentityRepo,
    IReadonlyRepo<BasicAuthEntity> basicAuthRepo,
    IIdentityProviderSettingsProvider identityProviderSettingsProvider,
    TimeProvider timeProvider,
    ILogger<FederatedIdentityProcessor> logger
) : IFederatedIdentityProcessor
{
    private readonly IRepo<FederatedIdentityEntity> _federatedIdentityRepo =
        federatedIdentityRepo.ThrowIfNull(nameof(federatedIdentityRepo));
    private readonly IReadonlyRepo<BasicAuthEntity> _basicAuthRepo = basicAuthRepo.ThrowIfNull(
        nameof(basicAuthRepo)
    );
    private readonly IIdentityProviderSettingsProvider _identityProviderSettingsProvider =
        identityProviderSettingsProvider.ThrowIfNull(nameof(identityProviderSettingsProvider));
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));
    private readonly ILogger<FederatedIdentityProcessor> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<Guid?> FindUserIdAsync(string issuer, string subject) =>
        (
            await _federatedIdentityRepo.GetAsync(e => e.Issuer == issuer && e.Subject == subject)
        )?.UserId;

    public async Task<LinkFederatedIdentityResult> LinkAsync(Guid userId, ExternalIdentity identity)
    {
        if (
            await _federatedIdentityRepo.AnyAsync(e =>
                e.Issuer == identity.Issuer && e.Subject == identity.Subject
            )
        )
        {
            return new(
                LinkFederatedIdentityResultCode.IdentityInUseError,
                $"This {identity.Type} account is already linked to a user"
            );
        }

        if (
            await _federatedIdentityRepo.AnyAsync(e =>
                e.UserId == userId && e.ProviderType == identity.Type
            )
        )
        {
            return new(
                LinkFederatedIdentityResultCode.AlreadyLinkedError,
                $"A {identity.Type} account is already linked to this user"
            );
        }

        await _federatedIdentityRepo.CreateAsync(identity.ToEntity(userId));
        _logger.LogDebug("Linked a {Type} identity to user {UserId}", identity.Type, userId);
        return new(LinkFederatedIdentityResultCode.Success, string.Empty);
    }

    public Task<LinkFederatedIdentityResult> LinkToNewUserAsync(
        Guid userId,
        ExternalIdentity identity
    ) => LinkAsync(userId, identity);

    public async Task<UnlinkFederatedIdentityResult> UnlinkAsync(
        Guid userId,
        IdentityProviderType type
    )
    {
        var identities = await _federatedIdentityRepo.ListAsync(e => e.UserId == userId);
        var identity = identities.FirstOrDefault(e => e.ProviderType == type);
        if (identity is null)
        {
            return new(
                UnlinkFederatedIdentityResultCode.NotLinkedError,
                $"No {type} account is linked"
            );
        }

        var usableTypes = await UsableTypesAsync();
        var hasOtherWayIn =
            await _basicAuthRepo.AnyAsync(b => b.UserId == userId)
            || identities.Any(e => e.ProviderType != type && usableTypes.Contains(e.ProviderType));
        if (!hasOtherWayIn)
        {
            return new(
                UnlinkFederatedIdentityResultCode.LastAuthMethodError,
                "Cannot unlink the only way to sign in"
            );
        }

        await _federatedIdentityRepo.DeleteAsync(identity);
        _logger.LogDebug("Unlinked the {Type} identity of user {UserId}", type, userId);
        return new(UnlinkFederatedIdentityResultCode.Success, string.Empty);
    }

    public async Task<IReadOnlyList<LinkedIdentity>> ListAsync(Guid userId) =>
        [
            .. (await _federatedIdentityRepo.ListAsync(e => e.UserId == userId))
                .OrderBy(e => e.ProviderType)
                .Select(e => e.ToModel()),
        ];

    public async Task<bool> RecordReauthenticationAsync(
        Guid userId,
        ExternalIdentity identity,
        Guid sessionId
    )
    {
        var entity = await _federatedIdentityRepo.GetAsync(e =>
            e.UserId == userId && e.Issuer == identity.Issuer && e.Subject == identity.Subject
        );
        if (entity is null)
            return false;

        entity.ReauthenticatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        entity.ReauthenticatedSessionId = sessionId;
        await _federatedIdentityRepo.UpdateAsync(entity);
        return true;
    }

    public async Task<bool> ConsumeReauthenticationAsync(Guid userId, Guid sessionId)
    {
        var oldestAccepted = _timeProvider
            .GetUtcNow()
            .UtcDateTime.AddSeconds(-FederatedIdentityEntity.REAUTHENTICATION_MAX_AGE_SECONDS);
        var entity = await _federatedIdentityRepo.GetAsync(e =>
            e.UserId == userId
            && e.ReauthenticatedSessionId == sessionId
            && e.ReauthenticatedUtc != null
            && e.ReauthenticatedUtc >= oldestAccepted
        );
        if (entity is null)
            return false;

        entity.ReauthenticatedUtc = null;
        entity.ReauthenticatedSessionId = null;
        await _federatedIdentityRepo.UpdateAsync(entity);
        return true;
    }

    public Task<bool> HasAnyAsync(Guid userId) =>
        _federatedIdentityRepo.AnyAsync(e => e.UserId == userId);

    public async Task<int> CountSoleSignInUsersAsync(IdentityProviderType type)
    {
        var userIds = await _federatedIdentityRepo.QueryAsync(q =>
            q.Where(e => e.ProviderType == type).Select(e => e.UserId)
        );
        if (userIds.Count == 0)
            return 0;

        var withPassword = await _basicAuthRepo.QueryAsync(q =>
            q.Where(b => userIds.Contains(b.UserId)).Select(b => b.UserId)
        );
        var otherTypes = (await UsableTypesAsync()).Where(t => t != type).ToList();
        var withOtherIdentity = await _federatedIdentityRepo.QueryAsync(q =>
            q.Where(e => userIds.Contains(e.UserId) && otherTypes.Contains(e.ProviderType))
                .Select(e => e.UserId)
        );

        return userIds.Except(withPassword).Except(withOtherIdentity).Count();
    }

    private async Task<HashSet<IdentityProviderType>> UsableTypesAsync() =>
        [
            .. (await _identityProviderSettingsProvider.GetAllAsync())
                .Where(s => s.IsUsable)
                .Select(s => s.Type),
        ];
}
