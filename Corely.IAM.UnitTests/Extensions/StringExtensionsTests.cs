using System.Text;
using Corely.IAM.Extensions;

namespace Corely.IAM.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("MY======", "f")]
    [InlineData("MZXW6YTBOI", "foobar")]
    [InlineData("mzxw6ytboi", "foobar")]
    [InlineData("", "")]
    public void FromBase32_DecodesIgnoringPaddingAndCase(string input, string expected)
    {
        Assert.Equal(expected, Encoding.ASCII.GetString(input.FromBase32()));
    }

    [Fact]
    public void FromBase32_RoundTripsToBase32()
    {
        byte[] bytes = [0, 1, 2, 250, 251, 252, 253, 254, 255, 17];

        Assert.Equal(bytes, bytes.ToBase32().FromBase32());
    }

    [Theory]
    [InlineData("MZ1")]
    [InlineData("MZ8")]
    [InlineData("MZ-")]
    public void FromBase32_Throws_OnCharactersOutsideTheAlphabet(string input)
    {
        Assert.Throws<ArgumentException>(() => input.FromBase32());
    }

    [Fact]
    public void ToDisplayRecoveryCode_SplitsAfterTheFourthCharacter()
    {
        Assert.Equal("ABCD-EFGH", "ABCDEFGH".ToDisplayRecoveryCode());
    }
}
