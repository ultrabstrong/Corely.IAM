using Corely.IAM.Security.Enums;

namespace Corely.IAM.Audits.Providers;

internal interface IAuditAccessProvider
{
    Task<AuditAccess> GetAccessAsync(AuthAction action);
    Task<bool> HoldsInPlatformAccountAsync(AuthAction action, string resourceType);
}
