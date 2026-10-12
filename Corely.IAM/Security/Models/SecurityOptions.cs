using Corely.IAM.Platform.Constants;
using Corely.IAM.Platform.Models;
using Corely.Security.PasswordValidation.Models;

namespace Corely.IAM.Security.Models;

public class SecurityOptions
{
    public const string NAME = "SecurityOptions";
    public int MaxLoginAttempts { get; set; } = SecurityPolicyConstants.MAX_LOGIN_ATTEMPTS_DEFAULT;
    public int LockoutCooldownSeconds { get; set; } =
        SecurityPolicyConstants.LOCKOUT_COOLDOWN_SECONDS_DEFAULT;
    public int AuthTokenTtlSeconds { get; set; } =
        SecurityPolicyConstants.AUTH_TOKEN_TTL_SECONDS_DEFAULT;
    public int AuthSessionTtlSeconds { get; set; } =
        SecurityPolicyConstants.AUTH_SESSION_TTL_SECONDS_DEFAULT;
    public int MfaChallengeTimeoutSeconds { get; set; } =
        SecurityPolicyConstants.MFA_CHALLENGE_TIMEOUT_SECONDS_DEFAULT;
    public int TotpRecoveryCodeCount { get; set; } =
        SecurityPolicyConstants.TOTP_RECOVERY_CODE_COUNT_DEFAULT;

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
            MfaChallengeTimeoutSeconds,
            TotpRecoveryCodeCount
        );
}
