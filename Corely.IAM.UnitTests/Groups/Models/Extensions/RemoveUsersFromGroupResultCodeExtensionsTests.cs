using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Models.Extensions;
using Corely.IAM.Models;

namespace Corely.IAM.UnitTests.Groups.Models.Extensions;

public class RemoveUsersFromGroupResultCodeExtensionsTests
{
    [Theory]
    [InlineData(RemoveUsersFromGroupResultCode.Success, DeregisterUsersFromGroupResultCode.Success)]
    [InlineData(
        RemoveUsersFromGroupResultCode.PartialSuccess,
        DeregisterUsersFromGroupResultCode.PartialSuccess
    )]
    [InlineData(
        RemoveUsersFromGroupResultCode.GroupNotFoundError,
        DeregisterUsersFromGroupResultCode.GroupNotFoundError
    )]
    [InlineData(
        RemoveUsersFromGroupResultCode.UserIsSoleOwnerError,
        DeregisterUsersFromGroupResultCode.UserIsSoleOwnerError
    )]
    [InlineData(
        RemoveUsersFromGroupResultCode.UnauthorizedError,
        DeregisterUsersFromGroupResultCode.UnauthorizedError
    )]
    public void ToDeregisterUsersFromGroupResultCode_MapsCorrectly(
        Enum input,
        DeregisterUsersFromGroupResultCode expected
    )
    {
        var result = ((RemoveUsersFromGroupResultCode)input).ToDeregisterUsersFromGroupResultCode();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDeregisterUsersFromGroupResultCode_ThrowsForUnmappedValue()
    {
        var unmappedValue = (RemoveUsersFromGroupResultCode)999;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            unmappedValue.ToDeregisterUsersFromGroupResultCode()
        );

        Assert.Contains("Unmapped", ex.Message);
        Assert.Contains(nameof(RemoveUsersFromGroupResultCode), ex.Message);
    }

    [Fact]
    public void ToDeregisterUsersFromGroupResultCode_AllEnumValuesMapped()
    {
        var allValues = Enum.GetValues<RemoveUsersFromGroupResultCode>();

        foreach (var value in allValues)
        {
            var ex = Record.Exception(() => value.ToDeregisterUsersFromGroupResultCode());
            Assert.Null(ex);
        }
    }
}
