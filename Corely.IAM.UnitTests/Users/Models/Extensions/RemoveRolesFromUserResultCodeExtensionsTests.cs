using Corely.IAM.Models;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Models.Extensions;

namespace Corely.IAM.UnitTests.Users.Models.Extensions;

public class RemoveRolesFromUserResultCodeExtensionsTests
{
    [Theory]
    [InlineData(RemoveRolesFromUserResultCode.Success, DeregisterRolesFromUserResultCode.Success)]
    [InlineData(
        RemoveRolesFromUserResultCode.PartialSuccess,
        DeregisterRolesFromUserResultCode.PartialSuccess
    )]
    [InlineData(
        RemoveRolesFromUserResultCode.InvalidRoleIdsError,
        DeregisterRolesFromUserResultCode.InvalidRoleIdsError
    )]
    [InlineData(
        RemoveRolesFromUserResultCode.UserNotFoundError,
        DeregisterRolesFromUserResultCode.UserNotFoundError
    )]
    [InlineData(
        RemoveRolesFromUserResultCode.UserIsSoleOwnerError,
        DeregisterRolesFromUserResultCode.UserIsSoleOwnerError
    )]
    [InlineData(
        RemoveRolesFromUserResultCode.UnauthorizedError,
        DeregisterRolesFromUserResultCode.UnauthorizedError
    )]
    public void ToDeregisterRolesFromUserResultCode_MapsCorrectly(
        RemoveRolesFromUserResultCode input,
        DeregisterRolesFromUserResultCode expected
    )
    {
        var result = input.ToDeregisterRolesFromUserResultCode();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDeregisterRolesFromUserResultCode_ThrowsForUnmappedValue()
    {
        var unmappedValue = (RemoveRolesFromUserResultCode)999;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            unmappedValue.ToDeregisterRolesFromUserResultCode()
        );

        Assert.Contains("Unmapped", ex.Message);
        Assert.Contains(nameof(RemoveRolesFromUserResultCode), ex.Message);
    }

    [Fact]
    public void ToDeregisterRolesFromUserResultCode_AllEnumValuesMapped()
    {
        var allValues = Enum.GetValues<RemoveRolesFromUserResultCode>();

        foreach (var value in allValues)
        {
            var ex = Record.Exception(() => value.ToDeregisterRolesFromUserResultCode());
            Assert.Null(ex);
        }
    }
}
