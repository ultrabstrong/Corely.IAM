using System.Text;
using Corely.IAM.Extensions;

namespace Corely.IAM.UnitTests.Extensions;

public class ByteArrayExtensionsTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("f", "MY")]
    [InlineData("fo", "MZXQ")]
    [InlineData("foo", "MZXW6")]
    [InlineData("foob", "MZXW6YQ")]
    [InlineData("fooba", "MZXW6YTB")]
    [InlineData("foobar", "MZXW6YTBOI")]
    public void ToBase32_MatchesRfc4648WithoutPadding(string input, string expected)
    {
        Assert.Equal(expected, Encoding.ASCII.GetBytes(input).ToBase32());
    }
}
