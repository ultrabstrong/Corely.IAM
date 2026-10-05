using Corely.IAM.Extensions;
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
)
{
    public const string CSV_HEADER =
        "Id,OccurredUtc,ActorUserId,ActorName,Cohort,AccountId,AccountName,Source,Service,Operation,Action,ResourceType,ResourceIds,ResultCode,Details";

    public string? ActorName { get; init; }
    public string? AccountName { get; init; }

    public bool IsPlatformMember => Cohort == AuditCohort.PlatformMember;

    public string CsvRow() =>
        string.Join(
            ',',
            new[]
            {
                Id.ToString(),
                OccurredUtc.ToString("O"),
                ActorUserId?.ToString() ?? string.Empty,
                ActorName ?? string.Empty,
                Cohort.ToString(),
                AccountId?.ToString() ?? string.Empty,
                AccountName ?? string.Empty,
                Source,
                Service,
                Operation,
                Action.ToString(),
                ResourceType,
                string.Join(' ', ResourceIds),
                ResultCode,
                Details ?? string.Empty,
            }.Select(field => field.CsvField())
        );
}
