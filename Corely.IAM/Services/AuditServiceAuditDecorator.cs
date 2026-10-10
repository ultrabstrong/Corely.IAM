using Corely.Common.Extensions;
using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.Services;

internal class AuditServiceAuditDecorator(IAuditService inner, IAuditProvider auditProvider)
    : IAuditService
{
    private const string SERVICE = nameof(IAuditService);

    private readonly IAuditService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<RetrieveListResult<AuditEntry>> ListEntriesAsync(ListAuditEntriesRequest request) =>
        _auditProvider.RecordAsync(
            ReadAudit(request.Filter?.AccountId),
            () => _inner.ListEntriesAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RetrieveSingleResult<AuditEntry>> GetEntryAsync(Guid entryId) =>
        _auditProvider.RecordAsync(
            ReadAudit(null) with
            {
                ResourceIds = [entryId],
            },
            () => _inner.GetEntryAsync(entryId),
            r => AuditOutcome.Of(r.ResultCode) with { AccountId = r.Item?.AccountId }
        );

    public Task<ExportAuditEntriesResult> ExportEntriesAsync(AuditEntryFilter filter) =>
        _auditProvider.RecordAsync(
            ReadAudit(filter.AccountId),
            () => _inner.ExportEntriesAsync(filter),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ListAuditAccountsResult> ListAuditAccountsAsync(AuthAction action) =>
        _auditProvider.RecordAsync(
            ReadAudit(null),
            () => _inner.ListAuditAccountsAsync(action),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<PurgeAuditEntriesResult> CountPurgeableEntriesAsync(
        PurgeAuditEntriesRequest request
    ) =>
        _auditProvider.RecordAsync(
            ReadAudit(request.AccountId),
            () => _inner.CountPurgeableEntriesAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<PurgeAuditEntriesResult> PurgeEntriesAsync(PurgeAuditEntriesRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Delete, AuditConstants.AUDIT_RESOURCE_TYPE)
            {
                AccountId = request.AccountId ?? Guid.Empty,
            },
            () => _inner.PurgeEntriesAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<GetAccountAuditSettingsResult> GetAccountSettingsAsync(Guid accountId) =>
        _auditProvider.RecordAsync(
            AccountSettings(AuthAction.Read, accountId),
            () => _inner.GetAccountSettingsAsync(accountId),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ModifyResult> UpdateAccountSettingsAsync(
        UpdateAccountAuditSettingsRequest request
    ) =>
        _auditProvider.RecordAsync(
            AccountSettings(AuthAction.Update, request.AccountId),
            () => _inner.UpdateAccountSettingsAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<GetPlatformSettingsResult> GetPlatformSettingsAsync() =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Read, AuditConstants.PLATFORM_SETTINGS_RESOURCE_TYPE),
            () => _inner.GetPlatformSettingsAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ModifyResult> UpdatePlatformSettingsAsync(PlatformSettings settings) =>
        _auditProvider.RecordAsync(
            new AuditCall(
                SERVICE,
                AuthAction.Update,
                AuditConstants.PLATFORM_SETTINGS_RESOURCE_TYPE
            ),
            () => _inner.UpdatePlatformSettingsAsync(settings),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<DeleteExpiredAuditEntriesResult> DeleteExpiredEntriesAsync() =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Delete, AuditConstants.AUDIT_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => _inner.DeleteExpiredEntriesAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    private static AuditCall ReadAudit(Guid? accountId) =>
        new(SERVICE, AuthAction.Read, AuditConstants.AUDIT_RESOURCE_TYPE)
        {
            AccountId = accountId ?? Guid.Empty,
        };

    private static AuditCall AccountSettings(AuthAction action, Guid accountId) =>
        new(SERVICE, action, AuditConstants.AUDIT_SETTINGS_RESOURCE_TYPE)
        {
            AccountId = accountId,
            ResourceIds = [accountId],
        };
}
