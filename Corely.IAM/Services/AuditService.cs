using System.Text;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Mappers;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Models.Extensions;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Models;
using Corely.IAM.Platform.Constants;
using Corely.IAM.Platform.Entities;
using Corely.IAM.Platform.Mappers;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class AuditService(
    IRepo<AuditEntryEntity> entryRepo,
    IRepo<AccountAuditSettingsEntity> accountSettingsRepo,
    IRepo<PlatformSettingsEntity> platformSettingsRepo,
    IReadonlyRepo<AccountEntity> accountRepo,
    IReadonlyRepo<UserEntity> userRepo,
    IAuditAccessProvider auditAccessProvider,
    IAuditPolicy auditPolicy,
    AuditSettingsCache auditSettingsCache,
    TimeProvider timeProvider,
    ILogger<AuditService> logger
) : IAuditService
{
    private const int ID_BATCH_SIZE = 500;
    private const string CSV_LINE_BREAK = "\r\n";

    private readonly IRepo<AuditEntryEntity> _entryRepo = entryRepo.ThrowIfNull(nameof(entryRepo));
    private readonly IRepo<AccountAuditSettingsEntity> _accountSettingsRepo =
        accountSettingsRepo.ThrowIfNull(nameof(accountSettingsRepo));
    private readonly IRepo<PlatformSettingsEntity> _platformSettingsRepo =
        platformSettingsRepo.ThrowIfNull(nameof(platformSettingsRepo));
    private readonly IReadonlyRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IReadonlyRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IAuditAccessProvider _auditAccessProvider = auditAccessProvider.ThrowIfNull(
        nameof(auditAccessProvider)
    );
    private readonly IAuditPolicy _auditPolicy = auditPolicy.ThrowIfNull(nameof(auditPolicy));
    private readonly AuditSettingsCache _auditSettingsCache = auditSettingsCache.ThrowIfNull(
        nameof(auditSettingsCache)
    );
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));
    private readonly ILogger<AuditService> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<RetrieveListResult<AuditEntry>> ListEntriesAsync(
        ListAuditEntriesRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (request.Skip < 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Skip must be non-negative.");
        if (request.Take <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Take must be positive.");

        var access = await _auditAccessProvider.GetAccessAsync(AuthAction.Read);
        var visible = access.VisibleEntries();
        var matching = (request.Filter ?? new AuditEntryFilter()).ToPredicate();

        var entities = await _entryRepo.QueryAsync(q =>
            q.Where(visible)
                .Where(matching)
                .OrderByDescending(e => e.OccurredUtc)
                .ThenByDescending(e => e.Id)
                .Skip(request.Skip)
                .Take(request.Take)
        );
        var totalCount = await _entryRepo.EvaluateAsync(
            (q, ct) => q.Where(visible).Where(matching).CountAsync(ct)
        );

        var items = await WithNamesAsync([.. entities.Select(e => e.ToModel())]);
        return new RetrieveListResult<AuditEntry>(
            RetrieveResultCode.Success,
            string.Empty,
            PagedResult<AuditEntry>.Create(items, totalCount, request.Skip, request.Take)
        );
    }

    public async Task<RetrieveSingleResult<AuditEntry>> GetEntryAsync(Guid entryId)
    {
        var access = await _auditAccessProvider.GetAccessAsync(AuthAction.Read);
        var visible = access.VisibleEntries();

        var entity = (
            await _entryRepo.QueryAsync(q => q.Where(e => e.Id == entryId).Where(visible).Take(1))
        ).FirstOrDefault();
        if (entity is null)
        {
            return new RetrieveSingleResult<AuditEntry>(
                RetrieveResultCode.NotFoundError,
                $"Audit entry {entryId} not found",
                null,
                null
            );
        }

        var item = (await WithNamesAsync([entity.ToModel()])).Single();
        return new RetrieveSingleResult<AuditEntry>(
            RetrieveResultCode.Success,
            string.Empty,
            item,
            null
        );
    }

    public async Task<ExportAuditEntriesResult> ExportEntriesAsync(AuditEntryFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter, nameof(filter));

        var access = await _auditAccessProvider.GetAccessAsync(AuthAction.Read);
        var visible = access.VisibleEntries();
        var matching = filter.ToPredicate();

        var entities = await _entryRepo.QueryAsync(q =>
            q.Where(visible)
                .Where(matching)
                .OrderByDescending(e => e.OccurredUtc)
                .ThenByDescending(e => e.Id)
                .Take(AuditConstants.EXPORT_MAX_ENTRIES + 1)
        );

        var truncated = entities.Count > AuditConstants.EXPORT_MAX_ENTRIES;
        var items = await WithNamesAsync([
            .. entities.Take(AuditConstants.EXPORT_MAX_ENTRIES).Select(e => e.ToModel()),
        ]);

        var csv = new StringBuilder(AuditEntry.CSV_HEADER).Append(CSV_LINE_BREAK);
        foreach (var item in items)
            csv.Append(item.CsvRow()).Append(CSV_LINE_BREAK);

        return new ExportAuditEntriesResult(
            RetrieveResultCode.Success,
            truncated
                ? $"The export stopped at {AuditConstants.EXPORT_MAX_ENTRIES:N0} entries. Narrow the date range to export the rest."
                : string.Empty,
            csv.ToString(),
            items.Count,
            truncated
        );
    }

    public async Task<ListAuditAccountsResult> ListAuditAccountsAsync(AuthAction action)
    {
        var access = await _auditAccessProvider.GetAccessAsync(action);
        var accountIds = access.AccountIds.ToList();

        var accounts = await _accountRepo.QueryAsync(q =>
            (access.Everything ? q : q.Where(a => accountIds.Contains(a.Id)))
                .OrderBy(a => a.AccountName)
                .Select(a => new AuditAccountOption(a.Id, a.AccountName))
        );

        return new ListAuditAccountsResult(
            RetrieveResultCode.Success,
            string.Empty,
            accounts,
            access.Everything
        );
    }

    public async Task<PurgeAuditEntriesResult> CountPurgeableEntriesAsync(
        PurgeAuditEntriesRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var predicate = request.ToPredicate();
        var count = await _entryRepo.EvaluateAsync((q, ct) => q.Where(predicate).CountAsync(ct));
        return new PurgeAuditEntriesResult(
            PurgeAuditEntriesResultCode.Success,
            string.Empty,
            count
        );
    }

    public async Task<PurgeAuditEntriesResult> PurgeEntriesAsync(PurgeAuditEntriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var predicate = request.ToPredicate();
        var count = await _entryRepo.EvaluateAsync(
            (q, ct) => q.Where(predicate).ExecuteDeleteAsync(ct)
        );

        _logger.LogInformation(
            "Purged {Count} audit entries for account {AccountId} older than {OlderThanUtc}",
            count,
            request.AccountId,
            request.OlderThanUtc
        );
        return new PurgeAuditEntriesResult(
            PurgeAuditEntriesResultCode.Success,
            string.Empty,
            count
        );
    }

    public async Task<GetAccountAuditSettingsResult> GetAccountSettingsAsync(Guid accountId)
    {
        var platform = await _auditPolicy.GetPlatformSettingsAsync();
        var settings = await _auditPolicy.GetAccountSettingsAsync(accountId);
        return settings is null
            ? new GetAccountAuditSettingsResult(
                RetrieveResultCode.NotFoundError,
                $"Account {accountId} not found",
                null,
                platform.AuditAllowedActions,
                platform.AuditMaxRetentionDays
            )
            : new GetAccountAuditSettingsResult(
                RetrieveResultCode.Success,
                string.Empty,
                settings,
                platform.AuditAllowedActions,
                platform.AuditMaxRetentionDays
            );
    }

    public async Task<ModifyResult> UpdateAccountSettingsAsync(
        UpdateAccountAuditSettingsRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        if (!await _accountRepo.AnyAsync(a => a.Id == request.AccountId))
            return new ModifyResult(
                ModifyResultCode.NotFoundError,
                $"Account {request.AccountId} not found"
            );

        if (!request.AccountMemberActions.IsKnown() || !request.PlatformMemberActions.IsKnown())
            return new ModifyResult(ModifyResultCode.ValidationError, "Unknown audit actions");

        var platform = await _auditPolicy.GetPlatformSettingsAsync();
        if (request.RetentionDays < 0 || request.RetentionDays > platform.AuditMaxRetentionDays)
            return new ModifyResult(
                ModifyResultCode.ValidationError,
                $"Retention must be between 0 and {platform.AuditMaxRetentionDays} days"
            );

        var settings = new AccountAuditSettings(
            request.AccountId,
            request.AccountMemberActions,
            request.PlatformMemberActions,
            request.RetentionDays
        );
        var entity = await _accountSettingsRepo.GetAsync(s => s.AccountId == request.AccountId);
        if (entity is null)
        {
            await _accountSettingsRepo.CreateAsync(settings.ToEntity());
        }
        else
        {
            entity.Apply(settings);
            await _accountSettingsRepo.UpdateAsync(entity);
        }

        _auditSettingsCache.InvalidateAccount(request.AccountId);
        return new ModifyResult(ModifyResultCode.Success, string.Empty);
    }

    public async Task<GetPlatformSettingsResult> GetPlatformSettingsAsync() =>
        new(
            RetrieveResultCode.Success,
            string.Empty,
            await _auditPolicy.GetPlatformSettingsAsync()
        );

    public async Task<ModifyResult> UpdatePlatformSettingsAsync(PlatformSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));

        if (
            !settings.AuditAllowedActions.IsKnown()
            || !settings.SystemContextActions.IsKnown()
            || !settings.AccountlessActions.IsKnown()
        )
            return new ModifyResult(ModifyResultCode.ValidationError, "Unknown audit actions");

        if (settings.AuditMaxRetentionDays < 0)
            return new ModifyResult(
                ModifyResultCode.ValidationError,
                "Maximum retention cannot be negative"
            );

        var entity = await _platformSettingsRepo.GetAsync(p =>
            p.Id == PlatformConstants.PLATFORM_SETTINGS_ID
        );
        if (entity is null)
        {
            await _platformSettingsRepo.CreateAsync(settings.ToEntity());
        }
        else
        {
            entity.Apply(settings);
            await _platformSettingsRepo.UpdateAsync(entity);
        }

        _auditSettingsCache.InvalidatePlatform();
        return new ModifyResult(ModifyResultCode.Success, string.Empty);
    }

    public async Task<DeleteExpiredAuditEntriesResult> DeleteExpiredEntriesAsync()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var maxRetentionDays = await _auditPolicy.RetentionDaysAsync(null);

        var maxCutoff = now.AddDays(-maxRetentionDays);
        var deleted = await _entryRepo.EvaluateAsync(
            (q, ct) => q.Where(e => e.OccurredUtc < maxCutoff).ExecuteDeleteAsync(ct)
        );

        var accountIds = await _entryRepo.QueryAsync(q =>
            q.Where(e => e.AccountId != null).Select(e => e.AccountId!.Value).Distinct()
        );

        var byRetention = new Dictionary<int, List<Guid>>();
        foreach (var accountId in accountIds)
        {
            var days = await _auditPolicy.RetentionDaysAsync(accountId);
            if (days >= maxRetentionDays)
                continue;
            if (!byRetention.TryGetValue(days, out var ids))
                byRetention[days] = ids = [];
            ids.Add(accountId);
        }

        foreach (var (days, ids) in byRetention)
        {
            var cutoff = now.AddDays(-days);
            foreach (var batch in ids.Chunk(ID_BATCH_SIZE))
            {
                var batchIds = batch.ToList();
                deleted += await _entryRepo.EvaluateAsync(
                    (q, ct) =>
                        q.Where(e =>
                                e.AccountId != null
                                && batchIds.Contains(e.AccountId.Value)
                                && e.OccurredUtc < cutoff
                            )
                            .ExecuteDeleteAsync(ct)
                );
            }
        }

        _logger.LogInformation("Deleted {Count} expired audit entries", deleted);
        return new DeleteExpiredAuditEntriesResult(
            DeleteExpiredAuditEntriesResultCode.Success,
            string.Empty,
            deleted
        );
    }

    private async Task<List<AuditEntry>> WithNamesAsync(List<AuditEntry> entries)
    {
        if (entries.Count == 0)
            return entries;

        var userNames = await UserNamesAsync([
            .. entries.Select(e => e.ActorUserId).OfType<Guid>().Distinct(),
        ]);
        var accountNames = await AccountNamesAsync([
            .. entries.Select(e => e.AccountId).OfType<Guid>().Distinct(),
        ]);

        return
        [
            .. entries.Select(e =>
                e with
                {
                    ActorName = e.ActorUserId is { } u ? userNames.GetValueOrDefault(u) : null,
                    AccountName = e.AccountId is { } a ? accountNames.GetValueOrDefault(a) : null,
                }
            ),
        ];
    }

    private async Task<Dictionary<Guid, string>> UserNamesAsync(List<Guid> userIds)
    {
        if (userIds.Count == 0)
            return [];

        var names = (
            await _userRepo.QueryAsync(q =>
                q.Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.Username })
            )
        ).ToDictionary(u => u.Id, u => u.Username);

        var missing = userIds.Where(id => !names.ContainsKey(id)).ToList();
        if (missing.Count == 0)
            return names;

        var deletions = await _entryRepo.QueryAsync(q =>
            q.Where(e =>
                    e.Service == nameof(IDeregistrationService)
                    && e.Operation == nameof(IDeregistrationService.DeregisterUserAsync)
                    && e.ResultCode == nameof(DeregisterUserResultCode.Success)
                    && e.ActorUserId != null
                    && missing.Contains(e.ActorUserId.Value)
                    && e.Details != null
                )
                .Select(e => new { UserId = e.ActorUserId!.Value, e.Details })
        );
        foreach (var deletion in deletions)
            names.TryAdd(deletion.UserId, deletion.Details!);

        return names;
    }

    private async Task<Dictionary<Guid, string>> AccountNamesAsync(List<Guid> accountIds)
    {
        if (accountIds.Count == 0)
            return [];

        var names = (
            await _accountRepo.QueryAsync(q =>
                q.Where(a => accountIds.Contains(a.Id)).Select(a => new { a.Id, a.AccountName })
            )
        ).ToDictionary(a => a.Id, a => a.AccountName);

        var missing = accountIds.Where(id => !names.ContainsKey(id)).ToList();
        if (missing.Count == 0)
            return names;

        var deletions = await _entryRepo.QueryAsync(q =>
            q.Where(e =>
                    e.Service == nameof(IDeregistrationService)
                    && e.Operation == nameof(IDeregistrationService.DeregisterAccountAsync)
                    && e.ResultCode == nameof(DeregisterAccountResultCode.Success)
                    && e.AccountId != null
                    && missing.Contains(e.AccountId.Value)
                    && e.Details != null
                )
                .Select(e => new { AccountId = e.AccountId!.Value, e.Details })
        );
        foreach (var deletion in deletions)
            names.TryAdd(deletion.AccountId, deletion.Details!);

        return names;
    }
}
