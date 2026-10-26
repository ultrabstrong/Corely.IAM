using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Users.Entities;

internal sealed class UserAuthTokenEntityConfiguration
    : EntityConfigurationBase<UserAuthTokenEntity>
{
    public UserAuthTokenEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<UserAuthTokenEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.UserId).IsRequired();

        builder.Property(e => e.IssuedUtc).IsRequired();

        builder.Property(e => e.ExpiresUtc).IsRequired();

        builder.HasIndex(e => e.ExpiresUtc);

        builder.Property(e => e.SessionId).IsRequired();
        builder.HasIndex(e => e.SessionId);

        builder.Property(e => e.SessionStartedUtc).IsRequired();

        builder
            .Property(e => e.RefreshSecretHash)
            .IsRequired()
            .HasMaxLength(UserAuthTokenEntity.REFRESH_SECRET_HASH_MAX_LENGTH);

        builder.Property(e => e.RefreshExpiresUtc).IsRequired();
        builder.HasIndex(e => e.RefreshExpiresUtc);

        builder.Property(e => e.ReplacedByTokenId).IsRequired(false);

        builder
            .HasOne(e => e.User)
            .WithMany(u => u.AuthTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
