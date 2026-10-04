using Corely.IAM.Audits.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.Audits.Providers;

internal interface IAuditPolicy
{
    Task<PlatformSettings> GetPlatformSettingsAsync();
    Task<AccountAuditSettings?> GetAccountSettingsAsync(Guid accountId);
    Task<AuditActions> RecordedActionsAsync(AuditCohort cohort, Guid? accountId);
    Task<bool> IsRecordedAsync(AuditCohort cohort, Guid? accountId, AuthAction action);
    bool IsAlwaysRecorded(string service, string operation);
    Task<int> RetentionDaysAsync(Guid? accountId);
}
