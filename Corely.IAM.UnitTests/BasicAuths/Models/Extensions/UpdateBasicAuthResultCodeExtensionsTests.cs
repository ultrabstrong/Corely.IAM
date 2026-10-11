using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Models.Extensions;

namespace Corely.IAM.UnitTests.BasicAuths.Models.Extensions;

public class UpdateBasicAuthResultCodeExtensionsTests
{
    [Fact]
    public void ToModifyPasswordResultCode_MapsEveryValue_ToTheCodeOfTheSameName()
    {
        foreach (var value in Enum.GetValues<UpdateBasicAuthResultCode>())
        {
            Assert.Equal(value.ToString(), value.ToModifyPasswordResultCode().ToString());
        }
    }

    [Fact]
    public void ToModifyPasswordResultCode_ThrowsForUnmappedValue() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ((UpdateBasicAuthResultCode)999).ToModifyPasswordResultCode()
        );
}
