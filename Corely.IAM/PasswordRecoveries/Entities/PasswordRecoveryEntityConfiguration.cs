using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.PasswordRecoveries.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.PasswordRecoveries.Entities;

internal sealed class PasswordRecoveryEntityConfiguration
    : EntityConfigurationBase<PasswordRecoveryEntity>
{
    public PasswordRecoveryEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<PasswordRecoveryEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.UserId).IsRequired();

        builder
            .Property(e => e.SecretHash)
            .IsRequired()
            .HasMaxLength(PasswordRecoveryConstants.SECRET_HASH_MAX_LENGTH);

        builder.Property(e => e.ExpiresUtc).IsRequired();
        builder.Property(e => e.CompletedUtc).IsRequired(false);
        builder.Property(e => e.InvalidatedUtc).IsRequired(false);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.ExpiresUtc);

        builder
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
