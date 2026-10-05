using Corely.IAM.Security.Constants;

namespace Corely.IAM.Audits.Models;

public record AuditEntryFilter
{
    public Guid? AccountId { get; init; }
    public Guid? ActorUserId { get; init; }
    public AuditCohort? Cohort { get; init; }
    public AuthAction? Action { get; init; }
    public string? ResourceType { get; init; }
    public string? ResultCode { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public bool IncludePlatformMembers { get; init; } = true;
}
