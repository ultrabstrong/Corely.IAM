using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Users.Entities;

internal class UserAsymmetricKeyEntityConfiguration
    : EntityConfigurationBase<UserAsymmetricKeyEntity, int>
{
    public UserAsymmetricKeyEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<UserAsymmetricKeyEntity> builder)
    {
        builder.HasIndex(e => new { e.UserId, e.KeyUsedFor }).IsUnique();

        builder.Property(m => m.KeyUsedFor).HasConversion<string>();

        builder.Property(m => m.ProviderTypeCode).IsRequired();

        builder.Property(m => m.Version).IsRequired();

        builder.Property(m => m.PublicKey).IsRequired();

        builder.Property(m => m.EncryptedPrivateKey).IsRequired();
    }
}
