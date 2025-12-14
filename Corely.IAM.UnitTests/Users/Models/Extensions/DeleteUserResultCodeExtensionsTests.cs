using Corely.IAM.Models;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Models.Extensions;

namespace Corely.IAM.UnitTests.Users.Models.Extensions;

public class DeleteUserResultCodeExtensionsTests
{
    [Theory]
    [InlineData(DeleteUserResultCode.Success, DeregisterUserResultCode.Success)]
    [InlineData(DeleteUserResultCode.UserNotFoundError, DeregisterUserResultCode.UserNotFoundError)]
    [InlineData(
        DeleteUserResultCode.UserIsSoleAccountOwnerError,
        DeregisterUserResultCode.UserIsSoleAccountOwnerError
    )]
    [InlineData(DeleteUserResultCode.UnauthorizedError, DeregisterUserResultCode.UnauthorizedError)]
    public void ToDeregisterUserResultCode_MapsCorrectly(
        DeleteUserResultCode input,
        DeregisterUserResultCode expected
    )
    {
        var result = input.ToDeregisterUserResultCode();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDeregisterUserResultCode_ThrowsForUnmappedValue()
    {
        var unmappedValue = (DeleteUserResultCode)999;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            unmappedValue.ToDeregisterUserResultCode()
        );

        Assert.Contains("Unmapped", ex.Message);
        Assert.Contains(nameof(DeleteUserResultCode), ex.Message);
    }

    [Fact]
    public void ToDeregisterUserResultCode_AllEnumValuesMapped()
    {
        var allValues = Enum.GetValues<DeleteUserResultCode>();

        foreach (var value in allValues)
        {
            var ex = Record.Exception(() => value.ToDeregisterUserResultCode());
            Assert.Null(ex);
        }
    }
}
