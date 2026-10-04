using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Accounts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Audits.Entities;

internal sealed class AccountAuditSettingsEntityConfiguration
    : EntityConfigurationBase<AccountAuditSettingsEntity>
{
    public AccountAuditSettingsEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<AccountAuditSettingsEntity> builder)
    {
        builder.HasKey(e => e.AccountId);
        builder.Property(e => e.AccountId).ValueGeneratedNever();

        builder.Property(e => e.AccountMemberActions).IsRequired();
        builder.Property(e => e.PlatformMemberActions).IsRequired();
        builder.Property(e => e.RetentionDays).IsRequired();

        builder
            .HasOne<AccountEntity>()
            .WithOne()
            .HasForeignKey<AccountAuditSettingsEntity>(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
