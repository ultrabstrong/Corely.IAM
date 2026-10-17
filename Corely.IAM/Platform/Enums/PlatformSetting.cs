namespace Corely.IAM.Platform.Enums;

public enum PlatformSetting
{
    AuditEnabled,
    AuditMaxRetentionDays,
    AuditAllowedActions,
    SystemContextActions,
    AccountlessActions,
    PasswordMinimumLength,
    PasswordMaximumLength,
    RequireUppercase,
    RequireLowercase,
    RequireDigit,
    RequireNonAlphanumeric,
    MaxLoginAttempts,
    LockoutCooldownSeconds,
    AuthTokenTtlSeconds,
    AuthSessionTtlSeconds,
    MfaChallengeTimeoutSeconds,
    TotpRecoveryCodeCount,
}
