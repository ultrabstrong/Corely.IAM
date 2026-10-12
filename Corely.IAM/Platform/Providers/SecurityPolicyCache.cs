using Corely.IAM.Audits.Models;
using Corely.IAM.Platform.Models;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Platform.Providers;

internal sealed class SecurityPolicyCache(TimeProvider timeProvider, IOptions<AuditOptions> options)
{
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(options.Value.SettingsCacheTtlSeconds);
    private Cached? _policy;

    public bool TryGet(out SecurityPolicy policy)
    {
        var cached = _policy;
        policy = cached?.Value!;
        return cached is not null && timeProvider.GetUtcNow() - cached.CachedAtUtc < _ttl;
    }

    public void Set(SecurityPolicy policy) => _policy = new(policy, timeProvider.GetUtcNow());

    public void Invalidate() => _policy = null;

    private sealed record Cached(SecurityPolicy Value, DateTimeOffset CachedAtUtc);
}
