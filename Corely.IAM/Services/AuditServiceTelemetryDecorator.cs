using Corely.Common.Extensions;
using Corely.IAM.Audits.Models;
using Corely.IAM.Extensions;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Constants;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class AuditServiceTelemetryDecorator(
    IAuditService inner,
    ILogger<AuditServiceTelemetryDecorator> logger
) : IAuditService
{
    private readonly IAuditService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<AuditServiceTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<RetrieveListResult<AuditEntry>> ListEntriesAsync(
        ListAuditEntriesRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuditService),
            request,
            () => _inner.ListEntriesAsync(request)
        );

    public async Task<RetrieveSingleResult<AuditEntry>> GetEntryAsync(Guid entryId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuditService),
            entryId,
            () => _inner.GetEntryAsync(entryId)
        );

    public async Task<ExportAuditEntriesResult> ExportEntriesAsync(AuditEntryFilter filter) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuditService),
            filter,
            () => _inner.ExportEntriesAsync(filter)
        );

    public async Task<ListAuditAccountsResult> ListAuditAccountsAsync(AuthAction action) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuditService),
            action,
            () => _inner.ListAuditAccountsAsync(action)
        );

    public async Task<PurgeAuditEntriesResult> CountPurgeableEntriesAsync(
        PurgeAuditEntriesRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuditService),
            request,
            () => _inner.CountPurgeableEntriesAsync(request),
            logResult: true
        );

    public async Task<PurgeAuditEntriesResult> PurgeEntriesAsync(
        PurgeAuditEntriesRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuditService),
            request,
            () => _inner.PurgeEntriesAsync(request),
            logResult: true
        );

    public async Task<GetAccountAuditSettingsResult> GetAccountSettingsAsync(Guid accountId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuditService),
            accountId,
            () => _inner.GetAccountSettingsAsync(accountId),
            logResult: true
        );

    public async Task<ModifyResult> UpdateAccountSettingsAsync(
        UpdateAccountAuditSettingsRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuditService),
            request,
            () => _inner.UpdateAccountSettingsAsync(request),
            logResult: true
        );

    public async Task<GetPlatformSettingsResult> GetPlatformSettingsAsync() =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuditService),
            () => _inner.GetPlatformSettingsAsync(),
            logResult: true
        );

    public async Task<ModifyResult> UpdatePlatformSettingsAsync(PlatformSettings settings) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuditService),
            settings,
            () => _inner.UpdatePlatformSettingsAsync(settings),
            logResult: true
        );

    public async Task<DeleteExpiredAuditEntriesResult> DeleteExpiredEntriesAsync() =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuditService),
            () => _inner.DeleteExpiredEntriesAsync(),
            logResult: true
        );
}
