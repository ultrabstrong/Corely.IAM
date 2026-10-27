using Corely.IAM.Platform.Models;
using Corely.IAM.Platform.Validators;

namespace Corely.IAM.UnitTests.Platform.Validators;

public class SecurityPolicyValidatorTests
{
    private readonly SecurityPolicyValidator _validator = new();

    public static TheoryData<string, int, int> Ranges() =>
        new()
        {
            {
                nameof(SecurityPolicy.PasswordMinimumLength),
                SecurityPolicy.PASSWORD_MINIMUM_LENGTH_MIN,
                SecurityPolicy.PASSWORD_MINIMUM_LENGTH_MAX
            },
            {
                nameof(SecurityPolicy.PasswordMaximumLength),
                SecurityPolicy.PASSWORD_MAXIMUM_LENGTH_MIN,
                SecurityPolicy.PASSWORD_MAXIMUM_LENGTH_MAX
            },
            {
                nameof(SecurityPolicy.MaxLoginAttempts),
                SecurityPolicy.MAX_LOGIN_ATTEMPTS_MIN,
                SecurityPolicy.MAX_LOGIN_ATTEMPTS_MAX
            },
            {
                nameof(SecurityPolicy.LockoutCooldownSeconds),
                SecurityPolicy.LOCKOUT_COOLDOWN_SECONDS_MIN,
                SecurityPolicy.LOCKOUT_COOLDOWN_SECONDS_MAX
            },
            {
                nameof(SecurityPolicy.AuthTokenTtlSeconds),
                SecurityPolicy.AUTH_TOKEN_TTL_SECONDS_MIN,
                SecurityPolicy.AUTH_TOKEN_TTL_SECONDS_MAX
            },
            {
                nameof(SecurityPolicy.AuthSessionTtlSeconds),
                SecurityPolicy.AUTH_SESSION_TTL_SECONDS_MIN,
                SecurityPolicy.AUTH_SESSION_TTL_SECONDS_MAX
            },
            {
                nameof(SecurityPolicy.AuthSessionIdleTimeoutSeconds),
                SecurityPolicy.AUTH_SESSION_IDLE_TIMEOUT_SECONDS_MIN,
                SecurityPolicy.AUTH_SESSION_IDLE_TIMEOUT_SECONDS_MAX
            },
            {
                nameof(SecurityPolicy.RefreshReuseGraceSeconds),
                SecurityPolicy.REFRESH_REUSE_GRACE_SECONDS_MIN,
                SecurityPolicy.REFRESH_REUSE_GRACE_SECONDS_MAX
            },
            {
                nameof(SecurityPolicy.MfaChallengeTimeoutSeconds),
                SecurityPolicy.MFA_CHALLENGE_TIMEOUT_SECONDS_MIN,
                SecurityPolicy.MFA_CHALLENGE_TIMEOUT_SECONDS_MAX
            },
            {
                nameof(SecurityPolicy.ApiKeyMaxLifetimeDays),
                SecurityPolicy.API_KEY_MAX_LIFETIME_DAYS_MIN,
                SecurityPolicy.API_KEY_MAX_LIFETIME_DAYS_MAX
            },
            {
                nameof(SecurityPolicy.TotpRecoveryCodeCount),
                SecurityPolicy.TOTP_RECOVERY_CODE_COUNT_MIN,
                SecurityPolicy.TOTP_RECOVERY_CODE_COUNT_MAX
            },
        };

    private static SecurityPolicy With(string setting, int value)
    {
        var policy = SecurityPolicy.Default with
        {
            PasswordMaximumLength = SecurityPolicy.PASSWORD_MAXIMUM_LENGTH_MAX,
            AuthSessionTtlSeconds = SecurityPolicy.AUTH_SESSION_TTL_SECONDS_MAX,
        };
        return setting switch
        {
            nameof(SecurityPolicy.PasswordMinimumLength) => policy with
            {
                PasswordMinimumLength = value,
            },
            nameof(SecurityPolicy.PasswordMaximumLength) => policy with
            {
                PasswordMinimumLength = SecurityPolicy.PASSWORD_MINIMUM_LENGTH_MIN,
                PasswordMaximumLength = value,
            },
            nameof(SecurityPolicy.MaxLoginAttempts) => policy with { MaxLoginAttempts = value },
            nameof(SecurityPolicy.LockoutCooldownSeconds) => policy with
            {
                LockoutCooldownSeconds = value,
            },
            nameof(SecurityPolicy.AuthTokenTtlSeconds) => policy with
            {
                AuthTokenTtlSeconds = value,
            },
            nameof(SecurityPolicy.AuthSessionTtlSeconds) => policy with
            {
                AuthTokenTtlSeconds = SecurityPolicy.AUTH_TOKEN_TTL_SECONDS_MIN,
                AuthSessionTtlSeconds = value,
            },
            nameof(SecurityPolicy.AuthSessionIdleTimeoutSeconds) => policy with
            {
                AuthTokenTtlSeconds = SecurityPolicy.AUTH_TOKEN_TTL_SECONDS_MIN,
                AuthSessionIdleTimeoutSeconds = value,
            },
            nameof(SecurityPolicy.RefreshReuseGraceSeconds) => policy with
            {
                RefreshReuseGraceSeconds = value,
            },
            nameof(SecurityPolicy.MfaChallengeTimeoutSeconds) => policy with
            {
                MfaChallengeTimeoutSeconds = value,
            },
            nameof(SecurityPolicy.ApiKeyMaxLifetimeDays) => policy with
            {
                ApiKeyMaxLifetimeDays = value,
            },
            nameof(SecurityPolicy.TotpRecoveryCodeCount) => policy with
            {
                TotpRecoveryCodeCount = value,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(setting), setting, null),
        };
    }

    [Fact]
    public void Default_IsValid() =>
        Assert.True(_validator.Validate(SecurityPolicy.Default).IsValid);

    [Theory]
    [MemberData(nameof(Ranges))]
    public void EachLimit_AcceptsItsBounds(string setting, int min, int max)
    {
        Assert.True(_validator.Validate(With(setting, min)).IsValid, $"{setting} = {min}");
        Assert.True(_validator.Validate(With(setting, max)).IsValid, $"{setting} = {max}");
    }

    [Theory]
    [MemberData(nameof(Ranges))]
    public void EachLimit_RefusesValuesOutsideItsBounds(string setting, int min, int max)
    {
        Assert.False(_validator.Validate(With(setting, min - 1)).IsValid, $"{setting} = {min - 1}");
        Assert.False(_validator.Validate(With(setting, max + 1)).IsValid, $"{setting} = {max + 1}");
    }

    [Fact]
    public void PasswordLengthRanges_NeverLetTheMinimumExceedTheMaximum() =>
        Assert.True(
            SecurityPolicy.PASSWORD_MINIMUM_LENGTH_MAX <= SecurityPolicy.PASSWORD_MAXIMUM_LENGTH_MIN
        );

    [Fact]
    public void TokenLifetime_CannotExceedTheSessionLifetime()
    {
        var result = _validator.Validate(
            SecurityPolicy.Default with
            {
                AuthTokenTtlSeconds = 3601,
                AuthSessionTtlSeconds = 3600,
            }
        );

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage.Contains("session lifetime", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void TokenLifetime_CannotExceedTheIdleTimeout()
    {
        var result = _validator.Validate(
            SecurityPolicy.Default with
            {
                AuthTokenTtlSeconds = 3601,
                AuthSessionIdleTimeoutSeconds = 3600,
            }
        );

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage.Contains("idle timeout", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void IdleTimeout_MayExceedTheSessionLifetime() =>
        Assert.True(
            _validator
                .Validate(
                    SecurityPolicy.Default with
                    {
                        AuthSessionTtlSeconds = 3600,
                        AuthSessionIdleTimeoutSeconds = 86400,
                    }
                )
                .IsValid
        );
}
