using Corely.IAM.Audits.Models;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.Services;

public interface IAuditService
{
    Task<RetrieveListResult<AuditEntry>> ListEntriesAsync(ListAuditEntriesRequest request);
    Task<RetrieveSingleResult<AuditEntry>> GetEntryAsync(Guid entryId);
    Task<ExportAuditEntriesResult> ExportEntriesAsync(AuditEntryFilter filter);
    Task<ListAuditAccountsResult> ListAuditAccountsAsync(AuthAction action);
    Task<PurgeAuditEntriesResult> CountPurgeableEntriesAsync(PurgeAuditEntriesRequest request);
    Task<PurgeAuditEntriesResult> PurgeEntriesAsync(PurgeAuditEntriesRequest request);
    Task<GetAccountAuditSettingsResult> GetAccountSettingsAsync(Guid accountId);
    Task<ModifyResult> UpdateAccountSettingsAsync(UpdateAccountAuditSettingsRequest request);
    Task<GetPlatformSettingsResult> GetPlatformSettingsAsync();
    Task<ModifyResult> UpdatePlatformSettingsAsync(PlatformSettings settings);
    Task<DeleteExpiredAuditEntriesResult> DeleteExpiredEntriesAsync();
}
