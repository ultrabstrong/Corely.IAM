using System.Collections.Concurrent;
using Corely.IAM.Audits.Models;
using Corely.IAM.Platform.Models;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Audits.Providers;

internal sealed class AuditSettingsCache(TimeProvider timeProvider, IOptions<AuditOptions> options)
{
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(options.Value.SettingsCacheTtlSeconds);
    private readonly ConcurrentDictionary<Guid, Cached<AccountAuditSettings?>> _accounts = new();
    private Cached<PlatformSettings>? _platform;

    public bool TryGetPlatform(out PlatformSettings settings)
    {
        var cached = _platform;
        settings = cached?.Value!;
        return cached is not null && IsFresh(cached.CachedAtUtc);
    }

    public void SetPlatform(PlatformSettings settings) =>
        _platform = new(settings, timeProvider.GetUtcNow());

    public bool TryGetAccount(Guid accountId, out AccountAuditSettings? settings)
    {
        if (_accounts.TryGetValue(accountId, out var cached) && IsFresh(cached.CachedAtUtc))
        {
            settings = cached.Value;
            return true;
        }
        settings = null;
        return false;
    }

    public void SetAccount(Guid accountId, AccountAuditSettings? settings) =>
        _accounts[accountId] = new(settings, timeProvider.GetUtcNow());

    public void InvalidatePlatform() => _platform = null;

    public void InvalidateAccount(Guid accountId) => _accounts.TryRemove(accountId, out _);

    private bool IsFresh(DateTimeOffset cachedAtUtc) =>
        timeProvider.GetUtcNow() - cachedAtUtc < _ttl;

    private sealed record Cached<T>(T Value, DateTimeOffset CachedAtUtc);
}
