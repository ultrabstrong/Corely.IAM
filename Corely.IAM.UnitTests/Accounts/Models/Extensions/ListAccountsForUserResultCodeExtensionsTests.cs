using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Models.Extensions;
using Corely.IAM.Models;

namespace Corely.IAM.UnitTests.Accounts.Models.Extensions;

public class ListAccountsForUserResultCodeExtensionsTests
{
    [Theory]
    [InlineData(ListAccountsForUserResultCode.Success, RetrieveAccountsResultCode.Success)]
    [InlineData(
        ListAccountsForUserResultCode.UnauthorizedError,
        RetrieveAccountsResultCode.UnauthorizedError
    )]
    public void ToRetrieveAccountsResultCode_MapsCorrectly(
        ListAccountsForUserResultCode input,
        RetrieveAccountsResultCode expected
    )
    {
        var result = input.ToRetrieveAccountsResultCode();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToRetrieveAccountsResultCode_ThrowsForUnmappedValue()
    {
        var unmappedValue = (ListAccountsForUserResultCode)999;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            unmappedValue.ToRetrieveAccountsResultCode()
        );

        Assert.Contains("Unmapped", ex.Message);
        Assert.Contains(nameof(ListAccountsForUserResultCode), ex.Message);
    }

    [Fact]
    public void ToRetrieveAccountsResultCode_AllEnumValuesMapped()
    {
        var allValues = Enum.GetValues<ListAccountsForUserResultCode>();

        foreach (var value in allValues)
        {
            var ex = Record.Exception(() => value.ToRetrieveAccountsResultCode());
            Assert.Null(ex);
        }
    }
}
