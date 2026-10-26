using Corely.IAM.Users.Models;

namespace Corely.IAM.UnitTests.Users.Models;

public class RefreshTokenTests
{
    [Fact]
    public void ToString_ThenTryParse_RoundTrips()
    {
        var token = new RefreshToken(Guid.CreateVersion7(), "secret-value");

        Assert.True(RefreshToken.TryParse(token.ToString(), out var parsed));
        Assert.Equal(token, parsed);
    }

    [Fact]
    public void TryParse_KeepsDotsInTheSecret()
    {
        var id = Guid.CreateVersion7();

        Assert.True(RefreshToken.TryParse($"{id:N}.a.b", out var parsed));
        Assert.Equal(id, parsed.TokenId);
        Assert.Equal("a.b", parsed.Secret);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-dot-at-all")]
    [InlineData("not-a-guid.secret")]
    [InlineData("0193b8a2-7c4e-7d3a-9f1e-2b5c8d9e0f1a.secret")]
    [InlineData("0193b8a27c4e7d3a9f1e2b5c8d9e0f1a.")]
    [InlineData("0193b8a27c4e7d3a9f1e2b5c8d9e0f1a.   ")]
    [InlineData(".secret")]
    public void TryParse_RefusesMalformedTokens(string? token)
    {
        Assert.False(RefreshToken.TryParse(token, out var parsed));
        Assert.Equal(Guid.Empty, parsed.TokenId);
    }

    [Fact]
    public void TryParse_RefusesAnAccessToken()
    {
        const string jwt = "eyJhbGciOiJFUzI1NiJ9.eyJzdWIiOiIxIn0.c2lnbmF0dXJl";

        Assert.False(RefreshToken.TryParse(jwt, out _));
    }
}
