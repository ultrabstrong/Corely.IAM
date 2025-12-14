using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Accounts.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Accounts.Entities;

internal sealed class AccountEntityConfiguration : EntityConfigurationBase<AccountEntity, int>
{
    public AccountEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.Property(e => e.PublicId).IsRequired();

        builder.HasIndex(e => e.PublicId).IsUnique();

        builder
            .Property(e => e.AccountName)
            .IsRequired()
            .HasMaxLength(AccountConstants.ACCOUNT_NAME_MAX_LENGTH);

        builder.HasIndex(e => e.AccountName).IsUnique();

        builder
            .HasMany(e => e.Groups)
            .WithOne(e => e.Account)
            .HasForeignKey(p => p.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.Roles)
            .WithOne(e => e.Account)
            .HasForeignKey(p => p.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.Permissions)
            .WithOne(e => e.Account)
            .HasForeignKey(p => p.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.SymmetricKeys)
            .WithOne()
            .HasForeignKey(p => p.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.AsymmetricKeys)
            .WithOne()
            .HasForeignKey(p => p.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
