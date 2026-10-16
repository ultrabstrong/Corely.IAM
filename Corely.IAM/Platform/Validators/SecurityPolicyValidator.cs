using Corely.IAM.Platform.Models;
using FluentValidation;

namespace Corely.IAM.Platform.Validators;

internal class SecurityPolicyValidator : AbstractValidator<SecurityPolicy>
{
    public SecurityPolicyValidator()
    {
        RuleFor(m => m.PasswordMinimumLength)
            .InclusiveBetween(
                SecurityPolicy.PASSWORD_MINIMUM_LENGTH_MIN,
                SecurityPolicy.PASSWORD_MINIMUM_LENGTH_MAX
            );
        RuleFor(m => m.PasswordMaximumLength)
            .InclusiveBetween(
                SecurityPolicy.PASSWORD_MAXIMUM_LENGTH_MIN,
                SecurityPolicy.PASSWORD_MAXIMUM_LENGTH_MAX
            );
        RuleFor(m => m.MaxLoginAttempts)
            .InclusiveBetween(
                SecurityPolicy.MAX_LOGIN_ATTEMPTS_MIN,
                SecurityPolicy.MAX_LOGIN_ATTEMPTS_MAX
            );
        RuleFor(m => m.LockoutCooldownSeconds)
            .InclusiveBetween(
                SecurityPolicy.LOCKOUT_COOLDOWN_SECONDS_MIN,
                SecurityPolicy.LOCKOUT_COOLDOWN_SECONDS_MAX
            );
        RuleFor(m => m.AuthTokenTtlSeconds)
            .InclusiveBetween(
                SecurityPolicy.AUTH_TOKEN_TTL_SECONDS_MIN,
                SecurityPolicy.AUTH_TOKEN_TTL_SECONDS_MAX
            );
        RuleFor(m => m.AuthSessionTtlSeconds)
            .InclusiveBetween(
                SecurityPolicy.AUTH_SESSION_TTL_SECONDS_MIN,
                SecurityPolicy.AUTH_SESSION_TTL_SECONDS_MAX
            );
        RuleFor(m => m.AuthTokenTtlSeconds)
            .LessThanOrEqualTo(m => m.AuthSessionTtlSeconds)
            .WithMessage("The sign in token lifetime cannot exceed the session lifetime");
        RuleFor(m => m.MfaChallengeTimeoutSeconds)
            .InclusiveBetween(
                SecurityPolicy.MFA_CHALLENGE_TIMEOUT_SECONDS_MIN,
                SecurityPolicy.MFA_CHALLENGE_TIMEOUT_SECONDS_MAX
            );
        RuleFor(m => m.TotpRecoveryCodeCount)
            .InclusiveBetween(
                SecurityPolicy.TOTP_RECOVERY_CODE_COUNT_MIN,
                SecurityPolicy.TOTP_RECOVERY_CODE_COUNT_MAX
            );
    }
}
