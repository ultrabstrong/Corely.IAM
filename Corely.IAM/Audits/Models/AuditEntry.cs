using Corely.IAM.Audits.Enums;
using Corely.IAM.Extensions;
using Corely.IAM.Security.Enums;

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
    public const string RESOURCE_TYPE = "audit";
    public const string SUCCESS_RESULT_CODE = "Success";
    public const string FAILED_RESULT_CODE = "Failed";
    public const string FAULT_RESULT_CODE = "Fault";
    public const string UNKNOWN_SOURCE = "unknown";

    internal const int SOURCE_MAX_LENGTH = 100;
    internal const int SERVICE_MAX_LENGTH = 100;
    internal const int OPERATION_MAX_LENGTH = 100;
    internal const int RESOURCE_TYPE_MAX_LENGTH = 100;
    internal const int RESULT_CODE_MAX_LENGTH = 100;
    internal const int DETAILS_MAX_LENGTH = 500;

    public const string CSV_HEADER =
        "Id,OccurredUtc,ActorUserId,ActorName,Cohort,AccountId,AccountName,Source,Service,Operation,Action,ResourceType,ResourceIds,ResultCode,Details";

    public Guid? ActorApiKeyId { get; init; }
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
