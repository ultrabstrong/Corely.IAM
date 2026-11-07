using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.IdentityProviders.Entities;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Mappers;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.Platform.Entities;

namespace Corely.IAM.IdentityProviders.Providers;

internal class IdentityProviderSettingsProvider(
    IReadonlyRepo<IdentityProviderEntity> identityProviderRepo,
    IReadonlyRepo<PlatformSecuritySettingsEntity> securitySettingsRepo,
    ConfiguredIdentityProviders configuredProviders,
    IdentityProviderSettingsCache cache,
    IdentityProviderCredentialCipher cipher
) : IIdentityProviderSettingsProvider
{
    private readonly IReadonlyRepo<IdentityProviderEntity> _identityProviderRepo =
        identityProviderRepo.ThrowIfNull(nameof(identityProviderRepo));
    private readonly IReadonlyRepo<PlatformSecuritySettingsEntity> _securitySettingsRepo =
        securitySettingsRepo.ThrowIfNull(nameof(securitySettingsRepo));
    private readonly ConfiguredIdentityProviders _configuredProviders =
        configuredProviders.ThrowIfNull(nameof(configuredProviders));
    private readonly IdentityProviderSettingsCache _cache = cache.ThrowIfNull(nameof(cache));
    private readonly IdentityProviderCredentialCipher _cipher = cipher.ThrowIfNull(nameof(cipher));

    public async Task<IReadOnlyList<IdentityProviderSettings>> GetAllAsync() =>
        [.. (await GetStoredAsync()).Select(p => p.Settings)];

    public async Task<ResolvedIdentityProvider?> GetUsableAsync(IdentityProviderType type)
    {
        var stored = (await GetStoredAsync()).FirstOrDefault(p => p.Settings.Type == type);
        if (stored is null || !stored.Settings.IsUsable)
            return null;

        var credential =
            stored.PlainCredential
            ?? (stored.EncryptedCredential is { } encrypted ? _cipher.Decrypt(encrypted) : null);
        return stored.Settings.CredentialKind == IdentityProviderCredentialKind.Certificate
            ? new ResolvedIdentityProvider(stored.Settings, null, credential)
            : new ResolvedIdentityProvider(stored.Settings, credential, null);
    }

    private async Task<IReadOnlyList<StoredIdentityProvider>> GetStoredAsync()
    {
        if (_cache.TryGet(out var cached))
            return cached;

        var platformSettingsSaved = await _securitySettingsRepo.AnyAsync(s =>
            s.Id == PlatformSecuritySettingsEntity.SINGLE_ROW_ID
        );
        IReadOnlyList<StoredIdentityProvider> providers = platformSettingsSaved
            ? [.. (await _identityProviderRepo.ListAsync(_ => true)).Select(e => e.ToStored())]
            : _configuredProviders.Providers;

        _cache.Set(providers);
        return providers;
    }
}
