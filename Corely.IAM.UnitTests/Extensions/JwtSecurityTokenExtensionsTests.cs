using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Corely.IAM.Extensions;
using Corely.IAM.Users.Constants;

namespace Corely.IAM.UnitTests.Extensions;

public class JwtSecurityTokenExtensionsTests
{
    private static JwtSecurityToken Token(params Claim[] claims) => new(claims: claims);

    private static Claim Unix(string type, DateTime utc) =>
        new(type, new DateTimeOffset(utc).ToUnixTimeSeconds().ToString());

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
    public void SessionStartedUtc_PrefersTheSessionClaim()
    {
        var started = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var token = Token(
            Unix(UserConstants.SESSION_STARTED_AT_CLAIM, started),
            Unix(JwtRegisteredClaimNames.Iat, started.AddHours(1))
        );

        Assert.Equal(started, token.SessionStartedUtc());
    }

    [Fact]
    public void SessionStartedUtc_FallsBackToIssuedAt()
    {
        var issued = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var token = Token(
            new Claim(UserConstants.SESSION_STARTED_AT_CLAIM, "not a number"),
            Unix(JwtRegisteredClaimNames.Iat, issued)
        );

        Assert.Equal(issued, token.SessionStartedUtc());
    }

    [Fact]
    public void SessionStartedUtc_ReturnsNull_WhenNeitherClaimReadable()
    {
        Assert.Null(Token().SessionStartedUtc());
    }

    [Fact]
    public void SignedInAccountId_ParsesTheClaim()
    {
        var id = Guid.CreateVersion7();
        var token = Token(new Claim(UserConstants.SIGNED_IN_ACCOUNT_ID_CLAIM, id.ToString()));

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
                : Token(new Claim(UserConstants.SIGNED_IN_ACCOUNT_ID_CLAIM, value));

        Assert.Null(token.SignedInAccountId());
    }
}
