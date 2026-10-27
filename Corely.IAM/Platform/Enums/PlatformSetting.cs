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
    AuthSessionIdleTimeoutSeconds,
    RefreshReuseGraceSeconds,
    MfaChallengeTimeoutSeconds,
    TotpRecoveryCodeCount,
    ApiKeysEnabled,
    PlatformAccountApiKeysEnabled,
    ApiKeyMaxLifetimeDays,
    ApiKeyResourceTypes,
}
