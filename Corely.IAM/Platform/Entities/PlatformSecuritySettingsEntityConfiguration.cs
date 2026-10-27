using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Platform.Entities;

internal sealed class PlatformSecuritySettingsEntityConfiguration
    : EntityConfigurationBase<PlatformSecuritySettingsEntity>
{
    public PlatformSecuritySettingsEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(
        EntityTypeBuilder<PlatformSecuritySettingsEntity> builder
    )
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.PasswordMinimumLength).IsRequired();
        builder.Property(e => e.PasswordMaximumLength).IsRequired();
        builder.Property(e => e.RequireUppercase).IsRequired();
        builder.Property(e => e.RequireLowercase).IsRequired();
        builder.Property(e => e.RequireDigit).IsRequired();
        builder.Property(e => e.RequireNonAlphanumeric).IsRequired();
        builder.Property(e => e.MaxLoginAttempts).IsRequired();
        builder.Property(e => e.LockoutCooldownSeconds).IsRequired();
        builder.Property(e => e.AuthTokenTtlSeconds).IsRequired();
        builder.Property(e => e.AuthSessionTtlSeconds).IsRequired();
        builder.Property(e => e.AuthSessionIdleTimeoutSeconds).IsRequired();
        builder.Property(e => e.RefreshReuseGraceSeconds).IsRequired();
        builder.Property(e => e.MfaChallengeTimeoutSeconds).IsRequired();
        builder.Property(e => e.TotpRecoveryCodeCount).IsRequired();
        builder.Property(e => e.ApiKeysEnabled).IsRequired();
        builder.Property(e => e.PlatformAccountApiKeysEnabled).IsRequired();
        builder.Property(e => e.ApiKeyMaxLifetimeDays).IsRequired();
        builder.Property(e => e.ApiKeyResourceTypes).IsRequired();
    }
}
