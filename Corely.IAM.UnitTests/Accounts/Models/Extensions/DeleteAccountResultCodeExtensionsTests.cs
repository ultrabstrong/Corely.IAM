using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Models.Extensions;
using Corely.IAM.Models;

namespace Corely.IAM.UnitTests.Accounts.Models.Extensions;

public class DeleteAccountResultCodeExtensionsTests
{
    [Theory]
    [InlineData(DeleteAccountResultCode.Success, DeregisterAccountResultCode.Success)]
    [InlineData(
        DeleteAccountResultCode.AccountNotFoundError,
        DeregisterAccountResultCode.AccountNotFoundError
    )]
    [InlineData(
        DeleteAccountResultCode.UnauthorizedError,
        DeregisterAccountResultCode.UnauthorizedError
    )]
    public void ToDeregisterAccountResultCode_MapsCorrectly(
        DeleteAccountResultCode input,
        DeregisterAccountResultCode expected
    )
    {
        var result = input.ToDeregisterAccountResultCode();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDeregisterAccountResultCode_ThrowsForUnmappedValue()
    {
        var unmappedValue = (DeleteAccountResultCode)999;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            unmappedValue.ToDeregisterAccountResultCode()
        );

        Assert.Contains("Unmapped", ex.Message);
        Assert.Contains(nameof(DeleteAccountResultCode), ex.Message);
    }

    [Fact]
    public void ToDeregisterAccountResultCode_AllEnumValuesMapped()
    {
        var allValues = Enum.GetValues<DeleteAccountResultCode>();

        foreach (var value in allValues)
        {
            var ex = Record.Exception(() => value.ToDeregisterAccountResultCode());
            Assert.Null(ex);
        }
    }
}
