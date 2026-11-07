using Corely.IAM.Audits.Models;
using Corely.IAM.IdentityProviders.Models;
using Microsoft.Extensions.Options;

namespace Corely.IAM.IdentityProviders.Providers;

internal sealed class IdentityProviderSettingsCache(
    TimeProvider timeProvider,
    IOptions<AuditOptions> options
)
{
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(options.Value.SettingsCacheTtlSeconds);
    private Cached? _providers;

    public bool TryGet(out IReadOnlyList<StoredIdentityProvider> providers)
    {
        var cached = _providers;
        providers = cached?.Value!;
        return cached is not null && timeProvider.GetUtcNow() - cached.CachedAtUtc < _ttl;
    }

    public void Set(IReadOnlyList<StoredIdentityProvider> providers) =>
        _providers = new(providers, timeProvider.GetUtcNow());

    public void Invalidate() => _providers = null;

    private sealed record Cached(
        IReadOnlyList<StoredIdentityProvider> Value,
        DateTimeOffset CachedAtUtc
    );
}
