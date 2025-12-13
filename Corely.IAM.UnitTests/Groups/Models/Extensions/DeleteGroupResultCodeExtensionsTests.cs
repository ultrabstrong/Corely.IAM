using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Models.Extensions;
using Corely.IAM.Models;

namespace Corely.IAM.UnitTests.Groups.Models.Extensions;

public class DeleteGroupResultCodeExtensionsTests
{
    [Theory]
    [InlineData(DeleteGroupResultCode.Success, DeregisterGroupResultCode.Success)]
    [InlineData(
        DeleteGroupResultCode.GroupNotFoundError,
        DeregisterGroupResultCode.GroupNotFoundError
    )]
    [InlineData(
        DeleteGroupResultCode.GroupHasSoleOwnersError,
        DeregisterGroupResultCode.GroupHasSoleOwnersError
    )]
    [InlineData(
        DeleteGroupResultCode.UnauthorizedError,
        DeregisterGroupResultCode.UnauthorizedError
    )]
    public void ToDeregisterGroupResultCode_MapsCorrectly(
        DeleteGroupResultCode input,
        DeregisterGroupResultCode expected
    )
    {
        var result = input.ToDeregisterGroupResultCode();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDeregisterGroupResultCode_ThrowsForUnmappedValue()
    {
        var unmappedValue = (DeleteGroupResultCode)999;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            unmappedValue.ToDeregisterGroupResultCode()
        );

        Assert.Contains("Unmapped", ex.Message);
        Assert.Contains(nameof(DeleteGroupResultCode), ex.Message);
    }

    [Fact]
    public void ToDeregisterGroupResultCode_AllEnumValuesMapped()
    {
        var allValues = Enum.GetValues<DeleteGroupResultCode>();

        foreach (var value in allValues)
        {
            var ex = Record.Exception(() => value.ToDeregisterGroupResultCode());
            Assert.Null(ex);
        }
    }
}
