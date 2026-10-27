using Corely.IAM.Platform.Models;

namespace Corely.IAM.UnitTests.Platform.Models;

public class SecurityPolicyTests
{
    [Fact]
    public void ToPasswordValidationOptions_CarriesEveryPasswordRule()
    {
        var policy = SecurityPolicy.Default with
        {
            PasswordMinimumLength = 10,
            PasswordMaximumLength = 100,
            RequireUppercase = false,
            RequireLowercase = false,
            RequireDigit = false,
            RequireNonAlphanumeric = true,
        };

        var options = policy.ToPasswordValidationOptions();

        Assert.Equal(10, options.MinimumLength);
        Assert.Equal(100, options.MaximumLength);
        Assert.False(options.RequireUppercase);
        Assert.False(options.RequireLowercase);
        Assert.False(options.RequireDigit);
        Assert.True(options.RequireNonAlphanumeric);
    }

    private static readonly DateTime _now = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

    [Fact]
    public void RefreshExpiresUtc_IsTheIdleTimeout_WhileTheSessionHasLongerLeft()
    {
        var policy = SecurityPolicy.Default with
        {
            AuthSessionIdleTimeoutSeconds = 600,
            AuthSessionTtlSeconds = 86400,
        };

        Assert.Equal(_now.AddSeconds(600), policy.RefreshExpiresUtc(_now, _now));
    }

    [Fact]
    public void RefreshExpiresUtc_IsTheSessionEnd_WhenThatComesFirst()
    {
        var policy = SecurityPolicy.Default with
        {
            AuthSessionIdleTimeoutSeconds = 600,
            AuthSessionTtlSeconds = 3600,
        };
        var sessionStarted = _now.AddSeconds(-3300);

        Assert.Equal(_now.AddSeconds(300), policy.RefreshExpiresUtc(sessionStarted, _now));
    }

    [Fact]
    public void AccessTokenExpiresUtc_IsTheTokenLifetime_WhenTheRefreshLastsLonger()
    {
        var policy = SecurityPolicy.Default with { AuthTokenTtlSeconds = 300 };

        Assert.Equal(_now.AddSeconds(300), policy.AccessTokenExpiresUtc(_now.AddHours(1), _now));
    }

    [Fact]
    public void AccessTokenExpiresUtc_NeverOutlivesTheRefreshToken()
    {
        var policy = SecurityPolicy.Default with { AuthTokenTtlSeconds = 3600 };
        var refreshExpires = _now.AddSeconds(60);

        Assert.Equal(refreshExpires, policy.AccessTokenExpiresUtc(refreshExpires, _now));
    }

    [Theory]
    [InlineData("Abcdefg1", true)]
    [InlineData("Abcdef1", false)]
    [InlineData("abcdefg1", false)]
    [InlineData("Abcdefgh", false)]
    public void ValidatePassword_AppliesTheDefaultRules(string password, bool expected) =>
        Assert.Equal(expected, SecurityPolicy.Default.ValidatePassword(password).IsSuccess);

    [Fact]
    public void ValidatePassword_AcceptsAShortPassword_WhenTheRulesAreRelaxed()
    {
        var relaxed = SecurityPolicy.Default with
        {
            PasswordMinimumLength = 3,
            RequireUppercase = false,
            RequireLowercase = false,
            RequireDigit = false,
        };

        Assert.True(relaxed.ValidatePassword("abc").IsSuccess);
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, false, true)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, true, true)]
    public void AllowsApiKeysIn_NeedsKeysOn_AndThePlatformSwitchInThePlatformAccount(
        bool apiKeysEnabled,
        bool platformAccountApiKeysEnabled,
        bool isPlatformAccount,
        bool expected
    ) =>
        Assert.Equal(
            expected,
            (
                SecurityPolicy.Default with
                {
                    ApiKeysEnabled = apiKeysEnabled,
                    PlatformAccountApiKeysEnabled = platformAccountApiKeysEnabled,
                }
            ).AllowsApiKeysIn(isPlatformAccount)
        );
}
