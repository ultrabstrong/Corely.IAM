using Corely.IAM.Audits.Models;

namespace Corely.IAM.Platform.Models;

public record PlatformSettings(
    bool AuditEnabled,
    int AuditMaxRetentionDays,
    AuditActions AuditAllowedActions,
    AuditActions SystemContextActions,
    AuditActions AccountlessActions
)
{
    public const string RESOURCE_TYPE = "platform_audit_settings";
    public const int DEFAULT_AUDIT_MAX_RETENTION_DAYS = 365;

    public static PlatformSettings Default { get; } =
        new(
            true,
            DEFAULT_AUDIT_MAX_RETENTION_DAYS,
            AuditActions.All,
            AuditActions.None,
            AuditActions.Create | AuditActions.Update | AuditActions.Delete | AuditActions.Execute
        );

    public AuditActions RecordedActions(AuditCohort cohort, AccountAuditSettings? account)
    {
        if (!AuditEnabled)
            return AuditActions.None;

        return cohort switch
        {
            AuditCohort.SystemContext => SystemContextActions,
            AuditCohort.Accountless => AccountlessActions,
            _ => account?.EffectiveActions(cohort, this) ?? AuditActions.None,
        };
    }
}
