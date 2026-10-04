using Corely.IAM.Security.Constants;

namespace Corely.IAM.Audits.Models;

public record AuditEntry(
    Guid Id,
    DateTime OccurredUtc,
    Guid? ActorUserId,
    AuditCohort Cohort,
    Guid? AccountId,
    string Source,
    string Service,
    string Operation,
    AuthAction Action,
    string ResourceType,
    IReadOnlyList<Guid> ResourceIds,
    string ResultCode,
    string? Details
);
