using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Models;

namespace Corely.IAM.Audits.Mappers;

internal static class AccountAuditSettingsMapper
{
    extension(AccountAuditSettingsEntity entity)
    {
        public AccountAuditSettings ToModel() =>
            new(
                entity.AccountId,
                entity.AccountMemberActions,
                entity.PlatformMemberActions,
                entity.RetentionDays
            );

        public void Apply(AccountAuditSettings settings)
        {
            entity.AccountMemberActions = settings.AccountMemberActions;
            entity.PlatformMemberActions = settings.PlatformMemberActions;
            entity.RetentionDays = settings.RetentionDays;
        }
    }

    extension(AccountAuditSettings settings)
    {
        public AccountAuditSettingsEntity ToEntity()
        {
            var entity = new AccountAuditSettingsEntity { AccountId = settings.AccountId };
            entity.Apply(settings);
            return entity;
        }
    }
}
