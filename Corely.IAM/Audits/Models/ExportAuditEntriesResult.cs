using Corely.IAM.Models;

namespace Corely.IAM.Audits.Models;

public record ExportAuditEntriesResult(
    RetrieveResultCode ResultCode,
    string Message,
    string? Csv,
    int ExportedCount,
    bool Truncated
)
{
    public const int MAX_ENTRIES = 10_000;
}
