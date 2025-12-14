using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Accounts.Entities;

internal class AccountAsymmetricKeyEntityConfiguration
    : EntityConfigurationBase<AccountAsymmetricKeyEntity, int>
{
    public AccountAsymmetricKeyEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<AccountAsymmetricKeyEntity> builder)
    {
        builder.HasIndex(e => new { e.AccountId, e.KeyUsedFor }).IsUnique();

        builder.Property(m => m.KeyUsedFor).HasConversion<string>();

        builder.Property(m => m.ProviderTypeCode).IsRequired();

        builder.Property(m => m.Version).IsRequired();

        builder.Property(m => m.PublicKey).IsRequired();

        builder.Property(m => m.EncryptedPrivateKey).IsRequired();
    }
}
