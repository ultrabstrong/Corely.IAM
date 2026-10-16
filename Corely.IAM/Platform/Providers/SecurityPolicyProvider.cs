using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Platform.Entities;
using Corely.IAM.Platform.Mappers;
using Corely.IAM.Platform.Models;

namespace Corely.IAM.Platform.Providers;

internal class SecurityPolicyProvider(
    IReadonlyRepo<PlatformSecuritySettingsEntity> settingsRepo,
    ConfiguredSecurityPolicy configuredPolicy,
    SecurityPolicyCache cache
) : ISecurityPolicyProvider
{
    private readonly IReadonlyRepo<PlatformSecuritySettingsEntity> _settingsRepo =
        settingsRepo.ThrowIfNull(nameof(settingsRepo));
    private readonly ConfiguredSecurityPolicy _configuredPolicy = configuredPolicy.ThrowIfNull(
        nameof(configuredPolicy)
    );
    private readonly SecurityPolicyCache _cache = cache.ThrowIfNull(nameof(cache));

    public async Task<SecurityPolicy> GetPolicyAsync()
    {
        if (_cache.TryGet(out var cached))
            return cached;

        var entity = await _settingsRepo.GetAsync(s =>
            s.Id == PlatformSecuritySettingsEntity.SINGLE_ROW_ID
        );
        var policy = entity?.ToModel() ?? _configuredPolicy.Policy;
        _cache.Set(policy);
        return policy;
    }
}
