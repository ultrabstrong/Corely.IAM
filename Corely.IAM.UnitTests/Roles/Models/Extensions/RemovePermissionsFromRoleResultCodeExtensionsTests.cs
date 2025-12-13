using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Models.Extensions;

namespace Corely.IAM.UnitTests.Roles.Models.Extensions;

public class RemovePermissionsFromRoleResultCodeExtensionsTests
{
    [Theory]
    [InlineData(
        RemovePermissionsFromRoleResultCode.Success,
        DeregisterPermissionsFromRoleResultCode.Success
    )]
    [InlineData(
        RemovePermissionsFromRoleResultCode.PartialSuccess,
        DeregisterPermissionsFromRoleResultCode.PartialSuccess
    )]
    [InlineData(
        RemovePermissionsFromRoleResultCode.InvalidPermissionIdsError,
        DeregisterPermissionsFromRoleResultCode.InvalidPermissionIdsError
    )]
    [InlineData(
        RemovePermissionsFromRoleResultCode.RoleNotFoundError,
        DeregisterPermissionsFromRoleResultCode.RoleNotFoundError
    )]
    [InlineData(
        RemovePermissionsFromRoleResultCode.SystemPermissionRemovalError,
        DeregisterPermissionsFromRoleResultCode.SystemPermissionRemovalError
    )]
    [InlineData(
        RemovePermissionsFromRoleResultCode.UnauthorizedError,
        DeregisterPermissionsFromRoleResultCode.UnauthorizedError
    )]
    public void ToDeregisterPermissionsFromRoleResultCode_MapsCorrectly(
        RemovePermissionsFromRoleResultCode input,
        DeregisterPermissionsFromRoleResultCode expected
    )
    {
        var result = input.ToDeregisterPermissionsFromRoleResultCode();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDeregisterPermissionsFromRoleResultCode_ThrowsForUnmappedValue()
    {
        var unmappedValue = (RemovePermissionsFromRoleResultCode)999;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            unmappedValue.ToDeregisterPermissionsFromRoleResultCode()
        );

        Assert.Contains("Unmapped", ex.Message);
        Assert.Contains(nameof(RemovePermissionsFromRoleResultCode), ex.Message);
    }

    [Fact]
    public void ToDeregisterPermissionsFromRoleResultCode_AllEnumValuesMapped()
    {
        var allValues = Enum.GetValues<RemovePermissionsFromRoleResultCode>();

        foreach (var value in allValues)
        {
            var ex = Record.Exception(() => value.ToDeregisterPermissionsFromRoleResultCode());
            Assert.Null(ex);
        }
    }
}
