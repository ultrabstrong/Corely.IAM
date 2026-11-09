using Corely.IAM.Invitations.Models;

namespace Corely.IAM.UnitTests.Invitations.Models;

public class InvitationTokenTests
{
    [Fact]
    public void TryParse_RoundTrips_ForToString()
    {
        var token = new InvitationToken(Guid.CreateVersion7(), "secret-part");

        Assert.True(InvitationToken.TryParse(token.ToString(), out var parsed));
        Assert.Equal(token, parsed);
    }

    [Fact]
    public void TryParse_TrimsWhitespace_ForPastedToken()
    {
        var token = new InvitationToken(Guid.CreateVersion7(), "secret-part");

        Assert.True(InvitationToken.TryParse($"  {token}\n", out var parsed));
        Assert.Equal(token, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-dot")]
    [InlineData("not-a-guid.secret")]
    [InlineData("0196a1b2c3d4e5f60718293a4b5c6d7e.")]
    public void TryParse_ReturnsFalse_ForMalformedToken(string? value)
    {
        Assert.False(InvitationToken.TryParse(value, out _));
    }
}
