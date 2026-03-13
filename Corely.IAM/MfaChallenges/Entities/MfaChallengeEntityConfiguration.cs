using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.MfaChallenges.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.MfaChallenges.Entities;

internal sealed class MfaChallengeEntityConfiguration : EntityConfigurationBase<MfaChallengeEntity>
{
    public MfaChallengeEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<MfaChallengeEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder
            .Property(e => e.ChallengeToken)
            .IsRequired()
            .HasMaxLength(MfaChallengeConstants.CHALLENGE_TOKEN_MAX_LENGTH);

        builder.HasIndex(e => e.ChallengeToken).IsUnique();

        builder
            .Property(e => e.DeviceId)
            .IsRequired()
            .HasMaxLength(MfaChallengeConstants.DEVICE_ID_MAX_LENGTH);

        builder.Property(e => e.AccountId).IsRequired(false);
        builder.Property(e => e.CompletedUtc).IsRequired(false);
        builder.Property(e => e.FailedAttempts).IsRequired().HasDefaultValue(0);

        builder.HasIndex(e => e.ExpiresUtc);
    }
}
