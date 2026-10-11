using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Models.Extensions;

namespace Corely.IAM.UnitTests.BasicAuths.Models.Extensions;

public class CreateBasicAuthResultCodeExtensionsTests
{
    [Fact]
    public void ToRegisterPasswordResultCode_MapsEveryValue_ToTheCodeOfTheSameName()
    {
        foreach (var value in Enum.GetValues<CreateBasicAuthResultCode>())
        {
            Assert.Equal(value.ToString(), value.ToRegisterPasswordResultCode().ToString());
        }
    }

    [Fact]
    public void ToRegisterPasswordResultCode_ThrowsForUnmappedValue() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ((CreateBasicAuthResultCode)999).ToRegisterPasswordResultCode()
        );
}
