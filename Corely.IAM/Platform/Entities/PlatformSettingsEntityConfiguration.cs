using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Platform.Entities;

internal sealed class PlatformSettingsEntityConfiguration
    : EntityConfigurationBase<PlatformSettingsEntity>
{
    public PlatformSettingsEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<PlatformSettingsEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.AuditEnabled).IsRequired();
        builder.Property(e => e.AuditMaxRetentionDays).IsRequired();
        builder.Property(e => e.AuditAllowedActions).IsRequired();
        builder.Property(e => e.SystemContextActions).IsRequired();
        builder.Property(e => e.AccountlessActions).IsRequired();
    }
}
