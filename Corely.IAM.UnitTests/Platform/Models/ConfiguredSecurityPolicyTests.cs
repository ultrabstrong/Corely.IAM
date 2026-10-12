using Corely.IAM.Platform.Models;
using Microsoft.Extensions.Configuration;

namespace Corely.IAM.UnitTests.Platform.Models;

public class ConfiguredSecurityPolicyTests
{
    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void From_EmptyConfiguration_IsTheDefaults_WithNoKeys()
    {
        var configured = ConfiguredSecurityPolicy.From(Configuration([]));

        Assert.Equal(SecurityPolicy.Default, configured.Policy);
        Assert.Empty(configured.ConfiguredKeys);
    }

    [Fact]
    public void From_ReadsBothSections_AndKeepsDefaultsForMissingKeys()
    {
        var configured = ConfiguredSecurityPolicy.From(
            Configuration(
                new()
                {
                    ["SecurityOptions:MaxLoginAttempts"] = "7",
                    ["SecurityOptions:TotpRecoveryCodeCount"] = "6",
                    ["PasswordValidationOptions:MinimumLength"] = "5",
                    ["PasswordValidationOptions:RequireDigit"] = "false",
                }
            )
        );

        Assert.Equal(
            SecurityPolicy.Default with
            {
                MaxLoginAttempts = 7,
                TotpRecoveryCodeCount = 6,
                PasswordMinimumLength = 5,
                RequireDigit = false,
            },
            configured.Policy
        );
    }

    [Fact]
    public void From_ListsOnlyTheKeysAPlatformAccountTakesOver()
    {
        var configured = ConfiguredSecurityPolicy.From(
            Configuration(
                new()
                {
                    ["SecurityOptions:MaxLoginAttempts"] = "7",
                    ["SecurityOptions:GoogleClientId"] = "client",
                    ["SecurityOptions:PermissionCacheTtlSeconds"] = "60",
                    ["PasswordValidationOptions:MinimumLength"] = "5",
                }
            )
        );

        Assert.Equal(
            ["SecurityOptions:MaxLoginAttempts", "PasswordValidationOptions:MinimumLength"],
            configured.ConfiguredKeys
        );
    }
}
