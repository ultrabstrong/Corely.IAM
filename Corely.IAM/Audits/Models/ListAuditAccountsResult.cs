using Corely.IAM.Models;

namespace Corely.IAM.Audits.Models;

public record ListAuditAccountsResult(
    RetrieveResultCode ResultCode,
    string Message,
    IReadOnlyList<AuditAccountOption> Accounts,
    bool IncludesEverything
);
