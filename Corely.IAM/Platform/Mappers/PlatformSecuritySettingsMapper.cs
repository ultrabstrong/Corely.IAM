using Corely.IAM.Platform.Entities;
using Corely.IAM.Platform.Models;

namespace Corely.IAM.Platform.Mappers;

internal static class PlatformSecuritySettingsMapper
{
    extension(PlatformSecuritySettingsEntity entity)
    {
        public SecurityPolicy ToModel() =>
            new(
                entity.PasswordMinimumLength,
                entity.PasswordMaximumLength,
                entity.RequireUppercase,
                entity.RequireLowercase,
                entity.RequireDigit,
                entity.RequireNonAlphanumeric,
                entity.MaxLoginAttempts,
                entity.LockoutCooldownSeconds,
                entity.AuthTokenTtlSeconds,
                entity.AuthSessionTtlSeconds,
                entity.AuthSessionIdleTimeoutSeconds,
                entity.RefreshReuseGraceSeconds,
                entity.MfaChallengeTimeoutSeconds,
                entity.TotpRecoveryCodeCount
            );

        public void Apply(SecurityPolicy policy)
        {
            entity.PasswordMinimumLength = policy.PasswordMinimumLength;
            entity.PasswordMaximumLength = policy.PasswordMaximumLength;
            entity.RequireUppercase = policy.RequireUppercase;
            entity.RequireLowercase = policy.RequireLowercase;
            entity.RequireDigit = policy.RequireDigit;
            entity.RequireNonAlphanumeric = policy.RequireNonAlphanumeric;
            entity.MaxLoginAttempts = policy.MaxLoginAttempts;
            entity.LockoutCooldownSeconds = policy.LockoutCooldownSeconds;
            entity.AuthTokenTtlSeconds = policy.AuthTokenTtlSeconds;
            entity.AuthSessionTtlSeconds = policy.AuthSessionTtlSeconds;
            entity.AuthSessionIdleTimeoutSeconds = policy.AuthSessionIdleTimeoutSeconds;
            entity.RefreshReuseGraceSeconds = policy.RefreshReuseGraceSeconds;
            entity.MfaChallengeTimeoutSeconds = policy.MfaChallengeTimeoutSeconds;
            entity.TotpRecoveryCodeCount = policy.TotpRecoveryCodeCount;
        }
    }

    extension(SecurityPolicy policy)
    {
        public PlatformSecuritySettingsEntity ToEntity()
        {
            var entity = new PlatformSecuritySettingsEntity
            {
                Id = PlatformSecuritySettingsEntity.SINGLE_ROW_ID,
            };
            entity.Apply(policy);
            return entity;
        }
    }
}
