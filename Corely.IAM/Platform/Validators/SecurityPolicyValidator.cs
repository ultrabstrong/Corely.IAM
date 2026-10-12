using Corely.IAM.Platform.Constants;
using Corely.IAM.Platform.Models;
using FluentValidation;

namespace Corely.IAM.Platform.Validators;

internal class SecurityPolicyValidator : AbstractValidator<SecurityPolicy>
{
    public SecurityPolicyValidator()
    {
        RuleFor(m => m.PasswordMinimumLength)
            .InclusiveBetween(
                SecurityPolicyConstants.PASSWORD_MINIMUM_LENGTH_MIN,
                SecurityPolicyConstants.PASSWORD_MINIMUM_LENGTH_MAX
            );
        RuleFor(m => m.PasswordMaximumLength)
            .InclusiveBetween(
                SecurityPolicyConstants.PASSWORD_MAXIMUM_LENGTH_MIN,
                SecurityPolicyConstants.PASSWORD_MAXIMUM_LENGTH_MAX
            );
        RuleFor(m => m.MaxLoginAttempts)
            .InclusiveBetween(
                SecurityPolicyConstants.MAX_LOGIN_ATTEMPTS_MIN,
                SecurityPolicyConstants.MAX_LOGIN_ATTEMPTS_MAX
            );
        RuleFor(m => m.LockoutCooldownSeconds)
            .InclusiveBetween(
                SecurityPolicyConstants.LOCKOUT_COOLDOWN_SECONDS_MIN,
                SecurityPolicyConstants.LOCKOUT_COOLDOWN_SECONDS_MAX
            );
        RuleFor(m => m.AuthTokenTtlSeconds)
            .InclusiveBetween(
                SecurityPolicyConstants.AUTH_TOKEN_TTL_SECONDS_MIN,
                SecurityPolicyConstants.AUTH_TOKEN_TTL_SECONDS_MAX
            );
        RuleFor(m => m.AuthSessionTtlSeconds)
            .InclusiveBetween(
                SecurityPolicyConstants.AUTH_SESSION_TTL_SECONDS_MIN,
                SecurityPolicyConstants.AUTH_SESSION_TTL_SECONDS_MAX
            );
        RuleFor(m => m.AuthTokenTtlSeconds)
            .LessThanOrEqualTo(m => m.AuthSessionTtlSeconds)
            .WithMessage("The sign in token lifetime cannot exceed the session lifetime");
        RuleFor(m => m.MfaChallengeTimeoutSeconds)
            .InclusiveBetween(
                SecurityPolicyConstants.MFA_CHALLENGE_TIMEOUT_SECONDS_MIN,
                SecurityPolicyConstants.MFA_CHALLENGE_TIMEOUT_SECONDS_MAX
            );
        RuleFor(m => m.TotpRecoveryCodeCount)
            .InclusiveBetween(
                SecurityPolicyConstants.TOTP_RECOVERY_CODE_COUNT_MIN,
                SecurityPolicyConstants.TOTP_RECOVERY_CODE_COUNT_MAX
            );
    }
}
