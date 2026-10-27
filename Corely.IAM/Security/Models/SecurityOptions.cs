using Corely.IAM.Platform.Models;
using Corely.Security.PasswordValidation.Models;

namespace Corely.IAM.Security.Models;

public class SecurityOptions
{
    public const string NAME = "SecurityOptions";
    public int MaxLoginAttempts { get; set; } = SecurityPolicy.MAX_LOGIN_ATTEMPTS_DEFAULT;
    public int LockoutCooldownSeconds { get; set; } =
        SecurityPolicy.LOCKOUT_COOLDOWN_SECONDS_DEFAULT;
    public int AuthTokenTtlSeconds { get; set; } = SecurityPolicy.AUTH_TOKEN_TTL_SECONDS_DEFAULT;
    public int AuthSessionTtlSeconds { get; set; } =
        SecurityPolicy.AUTH_SESSION_TTL_SECONDS_DEFAULT;
    public int AuthSessionIdleTimeoutSeconds { get; set; } =
        SecurityPolicy.AUTH_SESSION_IDLE_TIMEOUT_SECONDS_DEFAULT;
    public int RefreshReuseGraceSeconds { get; set; } =
        SecurityPolicy.REFRESH_REUSE_GRACE_SECONDS_DEFAULT;
    public int MfaChallengeTimeoutSeconds { get; set; } =
        SecurityPolicy.MFA_CHALLENGE_TIMEOUT_SECONDS_DEFAULT;
    public int TotpRecoveryCodeCount { get; set; } =
        SecurityPolicy.TOTP_RECOVERY_CODE_COUNT_DEFAULT;
    public bool ApiKeysEnabled { get; set; } = SecurityPolicy.API_KEYS_ENABLED_DEFAULT;
    public bool PlatformAccountApiKeysEnabled { get; set; } =
        SecurityPolicy.PLATFORM_ACCOUNT_API_KEYS_ENABLED_DEFAULT;
    public int ApiKeyMaxLifetimeDays { get; set; } =
        SecurityPolicy.API_KEY_MAX_LIFETIME_DAYS_DEFAULT;
    public string[] ApiKeyResourceTypes { get; set; } = [];

    public int PermissionCacheTtlSeconds { get; set; } = 30;
    public string? GoogleClientId { get; set; }

    internal SecurityPolicy ToSecurityPolicy(PasswordValidationOptions passwordOptions) =>
        new(
            passwordOptions.MinimumLength,
            passwordOptions.MaximumLength,
            passwordOptions.RequireUppercase,
            passwordOptions.RequireLowercase,
            passwordOptions.RequireDigit,
            passwordOptions.RequireNonAlphanumeric,
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
            new ResourceTypeSet(ApiKeyResourceTypes)
        );
}
