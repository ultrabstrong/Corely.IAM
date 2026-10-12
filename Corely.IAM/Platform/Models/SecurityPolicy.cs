using Corely.IAM.Platform.Constants;
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
    int MfaChallengeTimeoutSeconds,
    int TotpRecoveryCodeCount
)
{
    public static SecurityPolicy Default { get; } =
        new(
            SecurityPolicyConstants.PASSWORD_MINIMUM_LENGTH_DEFAULT,
            SecurityPolicyConstants.PASSWORD_MAXIMUM_LENGTH_DEFAULT,
            SecurityPolicyConstants.REQUIRE_UPPERCASE_DEFAULT,
            SecurityPolicyConstants.REQUIRE_LOWERCASE_DEFAULT,
            SecurityPolicyConstants.REQUIRE_DIGIT_DEFAULT,
            SecurityPolicyConstants.REQUIRE_NON_ALPHANUMERIC_DEFAULT,
            SecurityPolicyConstants.MAX_LOGIN_ATTEMPTS_DEFAULT,
            SecurityPolicyConstants.LOCKOUT_COOLDOWN_SECONDS_DEFAULT,
            SecurityPolicyConstants.AUTH_TOKEN_TTL_SECONDS_DEFAULT,
            SecurityPolicyConstants.AUTH_SESSION_TTL_SECONDS_DEFAULT,
            SecurityPolicyConstants.MFA_CHALLENGE_TIMEOUT_SECONDS_DEFAULT,
            SecurityPolicyConstants.TOTP_RECOVERY_CODE_COUNT_DEFAULT
        );

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
