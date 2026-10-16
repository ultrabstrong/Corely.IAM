using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.TotpAuths.Entities;

internal sealed class TotpRecoveryCodeEntityConfiguration
    : EntityConfigurationBase<TotpRecoveryCodeEntity>
{
    public TotpRecoveryCodeEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<TotpRecoveryCodeEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder
            .Property(e => e.CodeHash)
            .IsRequired()
            .HasMaxLength(TotpRecoveryCodeEntity.CODE_HASH_MAX_LENGTH);

        builder.Property(e => e.UsedUtc).IsRequired(false);

        builder
            .HasOne(e => e.TotpAuth)
            .WithMany(e => e.RecoveryCodes)
            .HasForeignKey(e => e.TotpAuthId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
