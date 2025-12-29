using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Security.Constants;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Accounts.Entities;

internal class AccountSymmetricKeyEntityConfiguration
    : EntityConfigurationBase<AccountSymmetricKeyEntity>
{
    public AccountSymmetricKeyEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<AccountSymmetricKeyEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.HasIndex(e => new { e.AccountId, e.KeyUsedFor }).IsUnique();

        builder.Property(e => e.KeyUsedFor).HasConversion<string>();

        builder.Property(e => e.ProviderTypeCode).IsRequired();

        builder.Property(e => e.Version).IsRequired();

        builder
            .Property(e => e.EncryptedKey)
            .IsRequired()
            .HasMaxLength(SymmetricKeyConstants.KEY_MAX_LENGTH);
    }
}
