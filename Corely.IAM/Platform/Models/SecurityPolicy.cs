using Corely.IAM.Platform.Enums;
using Corely.Security.PasswordValidation.Models;
using Corely.Security.PasswordValidation.Providers;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Platform.Models;

public record SecurityPolicy(
    int PasswordMinimumLength,
    int PasswordMaximumLength,
    bool RequireUppercase,
    bool RequireLowercase,
    bool RequireDigit,
    bool RequireNonAlphanumeric,
    int MaxLoginAttempts,
    int LockoutCooldownSeconds,
    int AuthTokenTtlSeconds,
    int AuthSessionTtlSeconds,
    int AuthSessionIdleTimeoutSeconds,
    int RefreshReuseGraceSeconds,
    int MfaChallengeTimeoutSeconds,
    int TotpRecoveryCodeCount,
    bool ApiKeysEnabled,
    bool PlatformAccountApiKeysEnabled,
    int ApiKeyMaxLifetimeDays,
    ResourceTypeSet ApiKeyResourceTypes
)
{
    public const string RESOURCE_TYPE = "platform_security_settings";

    public const int PASSWORD_MINIMUM_LENGTH_DEFAULT = 8;
    public const int PASSWORD_MINIMUM_LENGTH_MIN = 3;
    public const int PASSWORD_MINIMUM_LENGTH_MAX = 64;
    public const int PASSWORD_MINIMUM_LENGTH_RECOMMENDED = 8;

    public const int PASSWORD_MAXIMUM_LENGTH_DEFAULT = 128;
    public const int PASSWORD_MAXIMUM_LENGTH_MIN = 64;
    public const int PASSWORD_MAXIMUM_LENGTH_MAX = 256;

    public const bool REQUIRE_UPPERCASE_DEFAULT = true;
    public const bool REQUIRE_LOWERCASE_DEFAULT = true;
    public const bool REQUIRE_DIGIT_DEFAULT = true;
    public const bool REQUIRE_NON_ALPHANUMERIC_DEFAULT = false;

    public const int MAX_LOGIN_ATTEMPTS_DEFAULT = 5;
    public const int MAX_LOGIN_ATTEMPTS_MIN = 3;
    public const int MAX_LOGIN_ATTEMPTS_MAX = 20;

    public const int LOCKOUT_COOLDOWN_SECONDS_DEFAULT = 900;
    public const int LOCKOUT_COOLDOWN_SECONDS_MIN = 60;
    public const int LOCKOUT_COOLDOWN_SECONDS_MAX = 86400;

    public const int AUTH_TOKEN_TTL_SECONDS_DEFAULT = 3600;
    public const int AUTH_TOKEN_TTL_SECONDS_MIN = 300;
    public const int AUTH_TOKEN_TTL_SECONDS_MAX = 86400;

    public const int AUTH_SESSION_TTL_SECONDS_DEFAULT = 604800;
    public const int AUTH_SESSION_TTL_SECONDS_MIN = 3600;
    public const int AUTH_SESSION_TTL_SECONDS_MAX = 7776000;

    public const int AUTH_SESSION_IDLE_TIMEOUT_SECONDS_DEFAULT = 86400;
    public const int AUTH_SESSION_IDLE_TIMEOUT_SECONDS_MIN = 300;
    public const int AUTH_SESSION_IDLE_TIMEOUT_SECONDS_MAX = 7776000;

    public const int REFRESH_REUSE_GRACE_SECONDS_DEFAULT = 30;
    public const int REFRESH_REUSE_GRACE_SECONDS_MIN = 0;
    public const int REFRESH_REUSE_GRACE_SECONDS_MAX = 120;

    public const int MFA_CHALLENGE_TIMEOUT_SECONDS_DEFAULT = 300;
    public const int MFA_CHALLENGE_TIMEOUT_SECONDS_MIN = 60;
    public const int MFA_CHALLENGE_TIMEOUT_SECONDS_MAX = 900;

    public const int TOTP_RECOVERY_CODE_COUNT_DEFAULT = 10;
    public const int TOTP_RECOVERY_CODE_COUNT_MIN = 5;
    public const int TOTP_RECOVERY_CODE_COUNT_MAX = 20;

    public const bool API_KEYS_ENABLED_DEFAULT = false;
    public const bool PLATFORM_ACCOUNT_API_KEYS_ENABLED_DEFAULT = false;

    public const int API_KEY_MAX_LIFETIME_DAYS_DEFAULT = 365;
    public const int API_KEY_MAX_LIFETIME_DAYS_MIN = 1;
    public const int API_KEY_MAX_LIFETIME_DAYS_MAX = 3650;

    public static SecurityPolicy Default { get; } =
        new(
            PASSWORD_MINIMUM_LENGTH_DEFAULT,
            PASSWORD_MAXIMUM_LENGTH_DEFAULT,
            REQUIRE_UPPERCASE_DEFAULT,
            REQUIRE_LOWERCASE_DEFAULT,
            REQUIRE_DIGIT_DEFAULT,
            REQUIRE_NON_ALPHANUMERIC_DEFAULT,
            MAX_LOGIN_ATTEMPTS_DEFAULT,
            LOCKOUT_COOLDOWN_SECONDS_DEFAULT,
            AUTH_TOKEN_TTL_SECONDS_DEFAULT,
            AUTH_SESSION_TTL_SECONDS_DEFAULT,
            AUTH_SESSION_IDLE_TIMEOUT_SECONDS_DEFAULT,
            REFRESH_REUSE_GRACE_SECONDS_DEFAULT,
            MFA_CHALLENGE_TIMEOUT_SECONDS_DEFAULT,
            TOTP_RECOVERY_CODE_COUNT_DEFAULT,
            API_KEYS_ENABLED_DEFAULT,
            PLATFORM_ACCOUNT_API_KEYS_ENABLED_DEFAULT,
            API_KEY_MAX_LIFETIME_DAYS_DEFAULT,
            ResourceTypeSet.Empty
        );

    public bool AllowsApiKeysIn(bool isPlatformAccount) =>
        ApiKeysEnabled && (!isPlatformAccount || PlatformAccountApiKeysEnabled);

    internal DateTime ApiKeySecretExpiresUtc(DateTime? requestedUtc, DateTime utcNow) =>
        requestedUtc ?? utcNow.AddDays(ApiKeyMaxLifetimeDays);

    internal bool AllowsApiKeySecretExpiry(DateTime expiresUtc, DateTime utcNow) =>
        expiresUtc > utcNow && expiresUtc <= utcNow.AddDays(ApiKeyMaxLifetimeDays);

    public IReadOnlyList<SettingChange> ChangesTo(SecurityPolicy updated) =>
        new[]
        {
            SettingChange.Between(
                PlatformSetting.PasswordMinimumLength,
                PasswordMinimumLength,
                updated.PasswordMinimumLength,
                updated.PasswordMinimumLength < PasswordMinimumLength
            ),
            SettingChange.Between(
                PlatformSetting.PasswordMaximumLength,
                PasswordMaximumLength,
                updated.PasswordMaximumLength,
                false
            ),
            SettingChange.Between(
                PlatformSetting.RequireUppercase,
                RequireUppercase,
                updated.RequireUppercase,
                RequireUppercase && !updated.RequireUppercase
            ),
            SettingChange.Between(
                PlatformSetting.RequireLowercase,
                RequireLowercase,
                updated.RequireLowercase,
                RequireLowercase && !updated.RequireLowercase
            ),
            SettingChange.Between(
                PlatformSetting.RequireDigit,
                RequireDigit,
                updated.RequireDigit,
                RequireDigit && !updated.RequireDigit
            ),
            SettingChange.Between(
                PlatformSetting.RequireNonAlphanumeric,
                RequireNonAlphanumeric,
                updated.RequireNonAlphanumeric,
                RequireNonAlphanumeric && !updated.RequireNonAlphanumeric
            ),
            SettingChange.Between(
                PlatformSetting.MaxLoginAttempts,
                MaxLoginAttempts,
                updated.MaxLoginAttempts,
                updated.MaxLoginAttempts > MaxLoginAttempts
            ),
            SettingChange.Between(
                PlatformSetting.LockoutCooldownSeconds,
                LockoutCooldownSeconds,
                updated.LockoutCooldownSeconds,
                updated.LockoutCooldownSeconds < LockoutCooldownSeconds
            ),
            SettingChange.Between(
                PlatformSetting.AuthTokenTtlSeconds,
                AuthTokenTtlSeconds,
                updated.AuthTokenTtlSeconds,
                updated.AuthTokenTtlSeconds > AuthTokenTtlSeconds
            ),
            SettingChange.Between(
                PlatformSetting.AuthSessionTtlSeconds,
                AuthSessionTtlSeconds,
                updated.AuthSessionTtlSeconds,
                updated.AuthSessionTtlSeconds > AuthSessionTtlSeconds
            ),
            SettingChange.Between(
                PlatformSetting.AuthSessionIdleTimeoutSeconds,
                AuthSessionIdleTimeoutSeconds,
                updated.AuthSessionIdleTimeoutSeconds,
                updated.AuthSessionIdleTimeoutSeconds > AuthSessionIdleTimeoutSeconds
            ),
            SettingChange.Between(
                PlatformSetting.RefreshReuseGraceSeconds,
                RefreshReuseGraceSeconds,
                updated.RefreshReuseGraceSeconds,
                updated.RefreshReuseGraceSeconds > RefreshReuseGraceSeconds
            ),
            SettingChange.Between(
                PlatformSetting.MfaChallengeTimeoutSeconds,
                MfaChallengeTimeoutSeconds,
                updated.MfaChallengeTimeoutSeconds,
                false
            ),
            SettingChange.Between(
                PlatformSetting.TotpRecoveryCodeCount,
                TotpRecoveryCodeCount,
                updated.TotpRecoveryCodeCount,
                false
            ),
            SettingChange.Between(
                PlatformSetting.ApiKeysEnabled,
                ApiKeysEnabled,
                updated.ApiKeysEnabled,
                updated.ApiKeysEnabled
            ),
            SettingChange.Between(
                PlatformSetting.PlatformAccountApiKeysEnabled,
                PlatformAccountApiKeysEnabled,
                updated.PlatformAccountApiKeysEnabled,
                updated.PlatformAccountApiKeysEnabled
            ),
            SettingChange.Between(
                PlatformSetting.ApiKeyMaxLifetimeDays,
                ApiKeyMaxLifetimeDays,
                updated.ApiKeyMaxLifetimeDays,
                updated.ApiKeyMaxLifetimeDays > ApiKeyMaxLifetimeDays
            ),
            SettingChange.Between(
                PlatformSetting.ApiKeyResourceTypes,
                ApiKeyResourceTypes,
                updated.ApiKeyResourceTypes,
                updated.ApiKeyResourceTypes.Types.Any(t => !ApiKeyResourceTypes.Contains(t))
            ),
        }
            .OfType<SettingChange>()
            .OrderByDescending(change => change.Weakens)
            .ToList();

    internal DateTime RefreshExpiresUtc(DateTime sessionStartedUtc, DateTime now)
    {
        var idleExpiresUtc = now.AddSeconds(AuthSessionIdleTimeoutSeconds);
        var sessionExpiresUtc = sessionStartedUtc.AddSeconds(AuthSessionTtlSeconds);
        return idleExpiresUtc < sessionExpiresUtc ? idleExpiresUtc : sessionExpiresUtc;
    }

    internal DateTime AccessTokenExpiresUtc(DateTime refreshExpiresUtc, DateTime now)
    {
        var tokenExpiresUtc = now.AddSeconds(AuthTokenTtlSeconds);
        return tokenExpiresUtc < refreshExpiresUtc ? tokenExpiresUtc : refreshExpiresUtc;
    }

    internal PasswordValidationOptions ToPasswordValidationOptions() =>
        new()
        {
            MinimumLength = PasswordMinimumLength,
            MaximumLength = PasswordMaximumLength,
            RequireUppercase = RequireUppercase,
            RequireLowercase = RequireLowercase,
            RequireDigit = RequireDigit,
            RequireNonAlphanumeric = RequireNonAlphanumeric,
        };

    internal PasswordValidationResult ValidatePassword(string password) =>
        new PasswordValidationProvider(
            Options.Create(ToPasswordValidationOptions())
        ).ValidatePassword(password);
}
