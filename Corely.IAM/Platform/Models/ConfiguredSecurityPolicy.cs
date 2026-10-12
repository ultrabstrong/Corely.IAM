using Corely.IAM.Security.Models;
using Corely.Security.PasswordValidation.Models;
using Microsoft.Extensions.Configuration;

namespace Corely.IAM.Platform.Models;

internal sealed record ConfiguredSecurityPolicy(
    SecurityPolicy Policy,
    IReadOnlyList<string> ConfiguredKeys
)
{
    private static readonly string[] _movedSecurityOptionsKeys =
    [
        nameof(SecurityOptions.MaxLoginAttempts),
        nameof(SecurityOptions.LockoutCooldownSeconds),
        nameof(SecurityOptions.AuthTokenTtlSeconds),
        nameof(SecurityOptions.AuthSessionTtlSeconds),
        nameof(SecurityOptions.MfaChallengeTimeoutSeconds),
        nameof(SecurityOptions.TotpRecoveryCodeCount),
    ];

    public static ConfiguredSecurityPolicy From(IConfiguration configuration)
    {
        var securitySection = configuration.GetSection(SecurityOptions.NAME);
        var passwordSection = configuration.GetSection(PasswordValidationOptions.NAME);

        var securityOptions = new SecurityOptions();
        securitySection.Bind(securityOptions);
        var passwordOptions = SecurityPolicy.Default.ToPasswordValidationOptions();
        passwordSection.Bind(passwordOptions);

        string[] configuredKeys =
        [
            .. securitySection
                .GetChildren()
                .Where(c =>
                    _movedSecurityOptionsKeys.Contains(c.Key, StringComparer.OrdinalIgnoreCase)
                )
                .Select(c => c.Path),
            .. passwordSection.GetChildren().Select(c => c.Path),
        ];

        return new(securityOptions.ToSecurityPolicy(passwordOptions), configuredKeys);
    }
}
