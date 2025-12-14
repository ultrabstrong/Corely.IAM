using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Security.Constants;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Accounts.Entities;

internal class AccountSymmetricKeyEntityConfiguration
    : EntityConfigurationBase<AccountSymmetricKeyEntity, int>
{
    public AccountSymmetricKeyEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<AccountSymmetricKeyEntity> builder)
    {
        builder.HasIndex(e => new { e.AccountId, e.KeyUsedFor }).IsUnique();

        builder.Property(m => m.KeyUsedFor).HasConversion<string>();

        builder.Property(m => m.ProviderTypeCode).IsRequired();

        builder.Property(m => m.Version).IsRequired();

        builder
            .Property(m => m.EncryptedKey)
            .IsRequired()
            .HasMaxLength(SymmetricKeyConstants.KEY_MAX_LENGTH);
    }
}
