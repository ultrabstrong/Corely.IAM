using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Models.Extensions;

namespace Corely.IAM.UnitTests.Permissions.Models.Extensions;

public class DeletePermissionResultCodeExtensionsTests
{
    [Theory]
    [InlineData(DeletePermissionResultCode.Success, DeregisterPermissionResultCode.Success)]
    [InlineData(
        DeletePermissionResultCode.PermissionNotFoundError,
        DeregisterPermissionResultCode.PermissionNotFoundError
    )]
    [InlineData(
        DeletePermissionResultCode.SystemDefinedPermissionError,
        DeregisterPermissionResultCode.SystemDefinedPermissionError
    )]
    [InlineData(
        DeletePermissionResultCode.UnauthorizedError,
        DeregisterPermissionResultCode.UnauthorizedError
    )]
    public void ToDeregisterPermissionResultCode_MapsCorrectly(
        DeletePermissionResultCode input,
        DeregisterPermissionResultCode expected
    )
    {
        var result = input.ToDeregisterPermissionResultCode();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDeregisterPermissionResultCode_ThrowsForUnmappedValue()
    {
        var unmappedValue = (DeletePermissionResultCode)999;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            unmappedValue.ToDeregisterPermissionResultCode()
        );

        Assert.Contains("Unmapped", ex.Message);
        Assert.Contains(nameof(DeletePermissionResultCode), ex.Message);
    }

    [Fact]
    public void ToDeregisterPermissionResultCode_AllEnumValuesMapped()
    {
        var allValues = Enum.GetValues<DeletePermissionResultCode>();

        foreach (var value in allValues)
        {
            var ex = Record.Exception(() => value.ToDeregisterPermissionResultCode());
            Assert.Null(ex);
        }
    }
}
