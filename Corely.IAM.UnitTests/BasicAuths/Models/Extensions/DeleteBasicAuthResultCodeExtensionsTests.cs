using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Models.Extensions;

namespace Corely.IAM.UnitTests.BasicAuths.Models.Extensions;

public class DeleteBasicAuthResultCodeExtensionsTests
{
    [Fact]
    public void ToDeregisterBasicAuthResultCode_MapsEveryValue_ToTheCodeOfTheSameName()
    {
        foreach (var value in Enum.GetValues<DeleteBasicAuthResultCode>())
        {
            Assert.Equal(value.ToString(), value.ToDeregisterBasicAuthResultCode().ToString());
        }
    }

    [Fact]
    public void ToDeregisterBasicAuthResultCode_ThrowsForUnmappedValue() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ((DeleteBasicAuthResultCode)999).ToDeregisterBasicAuthResultCode()
        );
}
