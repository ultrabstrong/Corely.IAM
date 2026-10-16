using Corely.IAM.Platform.Models;

namespace Corely.IAM.Audits.Models;

public record AccountAuditSettings(
    Guid AccountId,
    AuditActions AccountMemberActions,
    AuditActions PlatformMemberActions,
    int RetentionDays
)
{
    public const string RESOURCE_TYPE = "audit_settings";
    public const int DEFAULT_RETENTION_DAYS = 90;

    public static AuditActions DefaultMemberActions =>
        AuditActions.Create | AuditActions.Update | AuditActions.Delete;

    public static AccountAuditSettings Default(Guid accountId) =>
        new(accountId, DefaultMemberActions, DefaultMemberActions, DEFAULT_RETENTION_DAYS);

    public AuditActions EffectiveActions(AuditCohort cohort, PlatformSettings platform) =>
        cohort switch
        {
            AuditCohort.AccountMember => AccountMemberActions & platform.AuditAllowedActions,
            AuditCohort.PlatformMember => PlatformMemberActions & platform.AuditAllowedActions,
            _ => AuditActions.None,
        };

    public int EffectiveRetentionDays(PlatformSettings platform) =>
        Math.Clamp(RetentionDays, 0, Math.Max(platform.AuditMaxRetentionDays, 0));
}
