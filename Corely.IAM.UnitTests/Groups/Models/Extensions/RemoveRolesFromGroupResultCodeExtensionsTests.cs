using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Models.Extensions;
using Corely.IAM.Models;

namespace Corely.IAM.UnitTests.Groups.Models.Extensions;

public class RemoveRolesFromGroupResultCodeExtensionsTests
{
    [Theory]
    [InlineData(RemoveRolesFromGroupResultCode.Success, DeregisterRolesFromGroupResultCode.Success)]
    [InlineData(
        RemoveRolesFromGroupResultCode.PartialSuccess,
        DeregisterRolesFromGroupResultCode.PartialSuccess
    )]
    [InlineData(
        RemoveRolesFromGroupResultCode.InvalidRoleIdsError,
        DeregisterRolesFromGroupResultCode.InvalidRoleIdsError
    )]
    [InlineData(
        RemoveRolesFromGroupResultCode.GroupNotFoundError,
        DeregisterRolesFromGroupResultCode.GroupNotFoundError
    )]
    [InlineData(
        RemoveRolesFromGroupResultCode.OwnerRoleRemovalBlockedError,
        DeregisterRolesFromGroupResultCode.OwnerRoleRemovalBlockedError
    )]
    [InlineData(
        RemoveRolesFromGroupResultCode.UnauthorizedError,
        DeregisterRolesFromGroupResultCode.UnauthorizedError
    )]
    public void ToDeregisterRolesFromGroupResultCode_MapsCorrectly(
        RemoveRolesFromGroupResultCode input,
        DeregisterRolesFromGroupResultCode expected
    )
    {
        var result = input.ToDeregisterRolesFromGroupResultCode();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDeregisterRolesFromGroupResultCode_ThrowsForUnmappedValue()
    {
        var unmappedValue = (RemoveRolesFromGroupResultCode)999;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            unmappedValue.ToDeregisterRolesFromGroupResultCode()
        );

        Assert.Contains("Unmapped", ex.Message);
        Assert.Contains(nameof(RemoveRolesFromGroupResultCode), ex.Message);
    }

    [Fact]
    public void ToDeregisterRolesFromGroupResultCode_AllEnumValuesMapped()
    {
        var allValues = Enum.GetValues<RemoveRolesFromGroupResultCode>();

        foreach (var value in allValues)
        {
            var ex = Record.Exception(() => value.ToDeregisterRolesFromGroupResultCode());
            Assert.Null(ex);
        }
    }
}
