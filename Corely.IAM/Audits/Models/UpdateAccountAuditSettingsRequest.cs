using Corely.IAM.Audits.Enums;

namespace Corely.IAM.Audits.Models;

public record UpdateAccountAuditSettingsRequest(
    Guid AccountId,
    AuditActions AccountMemberActions,
    AuditActions PlatformMemberActions,
    int RetentionDays
);
