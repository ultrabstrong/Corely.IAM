using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Security.Entities;

internal sealed class AuthTokenEntityConfiguration : EntityConfigurationBase<AuthTokenEntity>
{
    public AuthTokenEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<AuthTokenEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.UserId).IsRequired(false);
        builder.Property(e => e.ApiKeyId).IsRequired(false);
        builder.Property(e => e.Slot).IsRequired(false);

        builder.Property(e => e.DeviceId).IsRequired();

        builder.Property(e => e.IssuedUtc).IsRequired();

        builder.Property(e => e.ExpiresUtc).IsRequired();

        builder.HasIndex(e => e.ExpiresUtc);

        builder.Property(e => e.SessionId).IsRequired();
        builder.HasIndex(e => e.SessionId);

        builder.Property(e => e.SessionStartedUtc).IsRequired();

        builder
            .Property(e => e.RefreshSecretHash)
            .IsRequired()
            .HasMaxLength(AuthTokenEntity.REFRESH_SECRET_HASH_MAX_LENGTH);

        builder.Property(e => e.RefreshExpiresUtc).IsRequired();
        builder.HasIndex(e => e.RefreshExpiresUtc);

        builder.Property(e => e.ReplacedByTokenId).IsRequired(false);

        builder
            .HasOne(e => e.User)
            .WithMany(u => u.AuthTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.ApiKey)
            .WithMany(k => k.AuthTokens)
            .HasForeignKey(e => e.ApiKeyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
