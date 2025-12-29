using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Accounts.Entities;

internal class AccountAsymmetricKeyEntityConfiguration
    : EntityConfigurationBase<AccountAsymmetricKeyEntity>
{
    public AccountAsymmetricKeyEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<AccountAsymmetricKeyEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.HasIndex(e => new { e.AccountId, e.KeyUsedFor }).IsUnique();

        builder.Property(e => e.KeyUsedFor).HasConversion<string>();

        builder.Property(e => e.ProviderTypeCode).IsRequired();

        builder.Property(e => e.Version).IsRequired();

        builder.Property(e => e.PublicKey).IsRequired();

        builder.Property(e => e.EncryptedPrivateKey).IsRequired();
    }
}
