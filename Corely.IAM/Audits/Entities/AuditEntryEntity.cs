using Corely.IAM.Audits.Models;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.Audits.Entities;

internal class AuditEntryEntity
{
    public Guid Id { get; set; }
    public DateTime OccurredUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public AuditCohort Cohort { get; set; }
    public Guid? AccountId { get; set; }
    public string Source { get; set; } = null!;
    public string Service { get; set; } = null!;
    public string Operation { get; set; } = null!;
    public AuthAction Action { get; set; }
    public string ResourceType { get; set; } = null!;
    public string? ResourceIds { get; set; }
    public string ResultCode { get; set; } = null!;
    public string? Details { get; set; }
}
