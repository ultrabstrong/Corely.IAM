using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Mappers;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Models.Extensions;
using Corely.IAM.Platform.Constants;
using Corely.IAM.Platform.Entities;
using Corely.IAM.Platform.Mappers;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Services;

namespace Corely.IAM.Audits.Providers;

internal class AuditPolicy(
    IReadonlyRepo<PlatformSettingsEntity> platformSettingsRepo,
    IReadonlyRepo<AccountAuditSettingsEntity> accountSettingsRepo,
    IReadonlyRepo<AccountEntity> accountRepo,
    AuditSettingsCache cache
) : IAuditPolicy
{
    private static readonly HashSet<(string Service, string Operation)> _alwaysRecorded =
    [
        (nameof(IDeregistrationService), nameof(IDeregistrationService.DeregisterUserAsync)),
        (nameof(IDeregistrationService), nameof(IDeregistrationService.DeregisterAccountAsync)),
    ];

    private readonly IReadonlyRepo<PlatformSettingsEntity> _platformSettingsRepo =
        platformSettingsRepo.ThrowIfNull(nameof(platformSettingsRepo));
    private readonly IReadonlyRepo<AccountAuditSettingsEntity> _accountSettingsRepo =
        accountSettingsRepo.ThrowIfNull(nameof(accountSettingsRepo));
    private readonly IReadonlyRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly AuditSettingsCache _cache = cache.ThrowIfNull(nameof(cache));

    public async Task<PlatformSettings> GetPlatformSettingsAsync()
    {
        if (_cache.TryGetPlatform(out var cached))
            return cached;

        var entity = await _platformSettingsRepo.GetAsync(p =>
            p.Id == PlatformConstants.PLATFORM_SETTINGS_ID
        );
        var settings = entity?.ToModel() ?? PlatformSettings.Default;
        _cache.SetPlatform(settings);
        return settings;
    }

    public async Task<AccountAuditSettings?> GetAccountSettingsAsync(Guid accountId)
    {
        if (_cache.TryGetAccount(accountId, out var cached))
            return cached;

        var entity = await _accountSettingsRepo.GetAsync(s => s.AccountId == accountId);
        var settings =
            entity?.ToModel()
            ?? (
                await _accountRepo.AnyAsync(a => a.Id == accountId)
                    ? AccountAuditSettings.Default(accountId)
                    : null
            );
        _cache.SetAccount(accountId, settings);
        return settings;
    }

    public async Task<AuditActions> RecordedActionsAsync(AuditCohort cohort, Guid? accountId)
    {
        var platform = await GetPlatformSettingsAsync();
        var account = accountId is { } id ? await GetAccountSettingsAsync(id) : null;
        return platform.RecordedActions(cohort, account);
    }

    public async Task<bool> IsRecordedAsync(
        AuditCohort cohort,
        Guid? accountId,
        AuthAction action
    ) => (await RecordedActionsAsync(cohort, accountId)).Includes(action);

    public bool IsAlwaysRecorded(string service, string operation) =>
        _alwaysRecorded.Contains((service, operation));

    public async Task<int> RetentionDaysAsync(Guid? accountId)
    {
        var platform = await GetPlatformSettingsAsync();
        var account = accountId is { } id ? await GetAccountSettingsAsync(id) : null;
        return account?.EffectiveRetentionDays(platform)
            ?? Math.Max(platform.AuditMaxRetentionDays, 0);
    }
}
