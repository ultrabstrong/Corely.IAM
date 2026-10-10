namespace Corely.IAM.Audits.Models;

public record DeleteExpiredAuditEntriesResult(
    DeleteExpiredAuditEntriesResultCode ResultCode,
    string Message,
    int DeletedCount
);

public enum DeleteExpiredAuditEntriesResultCode
{
    Success,
    UnauthorizedError,
}
