using Corely.IAM.Platform.Constants;
using Corely.IAM.Platform.Entities;
using Corely.IAM.Platform.Models;

namespace Corely.IAM.Platform.Mappers;

internal static class PlatformSettingsMapper
{
    extension(PlatformSettingsEntity entity)
    {
        public PlatformSettings ToModel() =>
            new(
                entity.AuditEnabled,
                entity.AuditMaxRetentionDays,
                entity.AuditAllowedActions,
                entity.SystemContextActions,
                entity.AccountlessActions
            );

        public void Apply(PlatformSettings settings)
        {
            entity.AuditEnabled = settings.AuditEnabled;
            entity.AuditMaxRetentionDays = settings.AuditMaxRetentionDays;
            entity.AuditAllowedActions = settings.AuditAllowedActions;
            entity.SystemContextActions = settings.SystemContextActions;
            entity.AccountlessActions = settings.AccountlessActions;
        }
    }

    extension(PlatformSettings settings)
    {
        public PlatformSettingsEntity ToEntity()
        {
            var entity = new PlatformSettingsEntity { Id = PlatformConstants.PLATFORM_SETTINGS_ID };
            entity.Apply(settings);
            return entity;
        }
    }
}
