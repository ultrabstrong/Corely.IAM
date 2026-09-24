using Corely.IAM.PasswordRecoveries.Models;

namespace Corely.IAM.UnitTests.PasswordRecoveries.Models;

public class PasswordRecoveryTokenTests
{
    [Fact]
    public void ToString_JoinsTheUnhyphenatedIdAndSecret()
    {
        var id = Guid.CreateVersion7();

        Assert.Equal($"{id:N}.secret", new PasswordRecoveryToken(id, "secret").ToString());
    }

    [Fact]
    public void TryParse_RoundTripsToString()
    {
        var token = new PasswordRecoveryToken(Guid.CreateVersion7(), "s.e.c");

        Assert.True(PasswordRecoveryToken.TryParse(token.ToString(), out var parsed));
        Assert.Equal(token, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nodot")]
    [InlineData("not-a-guid.secret")]
    [InlineData("0197a1b2c3d4e5f60718293a4b5c6d7e.")]
    [InlineData("0197a1b2-c3d4-e5f6-0718-293a4b5c6d7e.secret")]
    [InlineData("0197a1b2c3d4e5f60718293a4b5c6d7e.   ")]
    public void TryParse_Rejects(string? token)
    {
        Assert.False(PasswordRecoveryToken.TryParse(token, out var parsed));
        Assert.Equal(Guid.Empty, parsed.RecoveryId);
        Assert.Equal(string.Empty, parsed.Secret);
    }
}
