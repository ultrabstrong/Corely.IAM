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
}
