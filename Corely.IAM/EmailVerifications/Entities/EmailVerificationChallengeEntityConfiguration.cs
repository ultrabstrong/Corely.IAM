using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.MfaChallenges.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.EmailVerifications.Entities;

internal sealed class EmailVerificationChallengeEntityConfiguration
    : EntityConfigurationBase<EmailVerificationChallengeEntity>
{
    public EmailVerificationChallengeEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(
        EntityTypeBuilder<EmailVerificationChallengeEntity> builder
    )
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.UserId).IsRequired();
        builder
            .Property(e => e.SecretHash)
            .IsRequired()
            .HasMaxLength(EmailVerificationEntity.SECRET_HASH_MAX_LENGTH);
        builder
            .Property(e => e.DeviceId)
            .IsRequired()
            .HasMaxLength(MfaChallengeEntity.DEVICE_ID_MAX_LENGTH);
        builder.Property(e => e.AccountId).IsRequired(false);
        builder.Property(e => e.ExpiresUtc).IsRequired();
        builder.Property(e => e.CompletedUtc).IsRequired(false);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.AccountId);

        builder
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
