using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Models.Extensions;
using Corely.IAM.Platform.Enums;

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

    public IReadOnlyList<SettingChange> ChangesTo(PlatformSettings updated) =>
        new[]
        {
            SettingChange.Between(
                PlatformSetting.AuditEnabled,
                AuditEnabled,
                updated.AuditEnabled,
                AuditEnabled && !updated.AuditEnabled
            ),
            SettingChange.Between(
                PlatformSetting.AuditMaxRetentionDays,
                AuditMaxRetentionDays,
                updated.AuditMaxRetentionDays,
                updated.AuditMaxRetentionDays < AuditMaxRetentionDays
            ),
            SettingChange.Between(
                PlatformSetting.AuditAllowedActions,
                AuditAllowedActions,
                updated.AuditAllowedActions,
                AuditAllowedActions.Dropped(updated.AuditAllowedActions) != AuditActions.None
            ),
            SettingChange.Between(
                PlatformSetting.SystemContextActions,
                SystemContextActions,
                updated.SystemContextActions,
                SystemContextActions.Dropped(updated.SystemContextActions) != AuditActions.None
            ),
            SettingChange.Between(
                PlatformSetting.AccountlessActions,
                AccountlessActions,
                updated.AccountlessActions,
                AccountlessActions.Dropped(updated.AccountlessActions) != AuditActions.None
            ),
        }
            .OfType<SettingChange>()
            .OrderByDescending(change => change.Weakens)
            .ToList();

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
