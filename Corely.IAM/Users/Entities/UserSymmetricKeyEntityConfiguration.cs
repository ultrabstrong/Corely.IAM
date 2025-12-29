using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Security.Constants;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Users.Entities;

internal class UserSymmetricKeyEntityConfiguration
    : EntityConfigurationBase<UserSymmetricKeyEntity>
{
    public UserSymmetricKeyEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<UserSymmetricKeyEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.HasIndex(e => new { e.UserId, e.KeyUsedFor }).IsUnique();

        builder.Property(m => m.KeyUsedFor).HasConversion<string>();

        builder.Property(m => m.ProviderTypeCode).IsRequired();

        builder.Property(m => m.Version).IsRequired();

        builder
            .Property(m => m.EncryptedKey)
            .IsRequired()
            .HasMaxLength(SymmetricKeyConstants.KEY_MAX_LENGTH);
    }
}
