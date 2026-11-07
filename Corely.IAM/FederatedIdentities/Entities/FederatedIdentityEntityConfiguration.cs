using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.FederatedIdentities.Entities;

internal sealed class FederatedIdentityEntityConfiguration
    : EntityConfigurationBase<FederatedIdentityEntity>
{
    public FederatedIdentityEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<FederatedIdentityEntity> builder)
    {
        builder.ToTable(FederatedIdentityEntity.TABLE_NAME);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder
            .Property(e => e.Issuer)
            .IsRequired()
            .HasMaxLength(FederatedIdentityEntity.ISSUER_MAX_LENGTH);
        builder
            .Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(FederatedIdentityEntity.SUBJECT_MAX_LENGTH);
        builder.Property(e => e.Email).HasMaxLength(User.EMAIL_MAX_LENGTH);

        builder.HasIndex(e => new { e.Issuer, e.Subject }).IsUnique();
        builder.HasIndex(e => new { e.UserId, e.ProviderType }).IsUnique();

        builder
            .HasOne(e => e.User)
            .WithMany(u => u.FederatedIdentities)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
