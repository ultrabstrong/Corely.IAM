using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.TotpAuths.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.TotpAuths.Entities;

internal sealed class TotpAuthEntityConfiguration : EntityConfigurationBase<TotpAuthEntity>
{
    public TotpAuthEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<TotpAuthEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.HasIndex(e => e.UserId).IsUnique();

        builder
            .Property(e => e.EncryptedSecret)
            .IsRequired()
            .HasMaxLength(TotpAuthConstants.ENCRYPTED_SECRET_MAX_LENGTH);

        builder.Property(e => e.IsEnabled).IsRequired().HasDefaultValue(false);
    }
}
