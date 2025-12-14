using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Models.Extensions;

namespace Corely.IAM.UnitTests.Roles.Models.Extensions;

public class DeleteRoleResultCodeExtensionsTests
{
    [Theory]
    [InlineData(DeleteRoleResultCode.Success, DeregisterRoleResultCode.Success)]
    [InlineData(DeleteRoleResultCode.RoleNotFoundError, DeregisterRoleResultCode.RoleNotFoundError)]
    [InlineData(
        DeleteRoleResultCode.SystemDefinedRoleError,
        DeregisterRoleResultCode.SystemDefinedRoleError
    )]
    [InlineData(DeleteRoleResultCode.UnauthorizedError, DeregisterRoleResultCode.UnauthorizedError)]
    public void ToDeregisterRoleResultCode_MapsCorrectly(
        DeleteRoleResultCode input,
        DeregisterRoleResultCode expected
    )
    {
        var result = input.ToDeregisterRoleResultCode();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDeregisterRoleResultCode_ThrowsForUnmappedValue()
    {
        var unmappedValue = (DeleteRoleResultCode)999;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            unmappedValue.ToDeregisterRoleResultCode()
        );

        Assert.Contains("Unmapped", ex.Message);
        Assert.Contains(nameof(DeleteRoleResultCode), ex.Message);
    }

    [Fact]
    public void ToDeregisterRoleResultCode_AllEnumValuesMapped()
    {
        var allValues = Enum.GetValues<DeleteRoleResultCode>();

        foreach (var value in allValues)
        {
            var ex = Record.Exception(() => value.ToDeregisterRoleResultCode());
            Assert.Null(ex);
        }
    }
}
