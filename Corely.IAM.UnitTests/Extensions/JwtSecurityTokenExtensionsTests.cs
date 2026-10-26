using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Corely.IAM.Extensions;
using Corely.IAM.Security.Models;

namespace Corely.IAM.UnitTests.Extensions;

public class JwtSecurityTokenExtensionsTests
{
    private static JwtSecurityToken Token(params Claim[] claims) => new(claims: claims);

    [Fact]
    public void ClaimValue_ReturnsTheFirstMatchingClaim()
    {
        var token = Token(new Claim("a", "1"), new Claim("a", "2"));

        Assert.Equal("1", token.ClaimValue("a"));
    }

    [Fact]
    public void ClaimValue_ReturnsNull_WhenAbsent()
    {
        Assert.Null(Token().ClaimValue("a"));
    }

    [Fact]
    public void SignedInAccountId_ParsesTheClaim()
    {
        var id = Guid.CreateVersion7();
        var token = Token(new Claim(TokenIssueContext.SIGNED_IN_ACCOUNT_ID_CLAIM, id.ToString()));

        Assert.Equal(id, token.SignedInAccountId());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a guid")]
    public void SignedInAccountId_ReturnsNull_WhenMissingOrUnreadable(string? value)
    {
        var token =
            value == null
                ? Token()
                : Token(new Claim(TokenIssueContext.SIGNED_IN_ACCOUNT_ID_CLAIM, value));

        Assert.Null(token.SignedInAccountId());
    }
}
