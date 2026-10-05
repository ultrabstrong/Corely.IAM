namespace Corely.IAM.Audits.Models;

public record PurgeAuditEntriesResult(
    PurgeAuditEntriesResultCode ResultCode,
    string Message,
    int Count
);

public enum PurgeAuditEntriesResultCode
{
    Success,
    UnauthorizedError,
}
