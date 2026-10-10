using Corely.IAM.Models;

namespace Corely.IAM.Audits.Models;

public record GetAccountAuditSettingsResult(
    RetrieveResultCode ResultCode,
    string Message,
    AccountAuditSettings? Settings,
    AuditActions AllowedActions,
    int MaxRetentionDays
);
