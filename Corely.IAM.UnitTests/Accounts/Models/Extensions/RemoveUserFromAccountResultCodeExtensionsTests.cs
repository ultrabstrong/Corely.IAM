using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Models.Extensions;
using Corely.IAM.Models;

namespace Corely.IAM.UnitTests.Accounts.Models.Extensions;

public class RemoveUserFromAccountResultCodeExtensionsTests
{
    [Theory]
    [InlineData(
        RemoveUserFromAccountResultCode.Success,
        DeregisterUserFromAccountResultCode.Success
    )]
    [InlineData(
        RemoveUserFromAccountResultCode.UserNotFoundError,
        DeregisterUserFromAccountResultCode.UserNotFoundError
    )]
    [InlineData(
        RemoveUserFromAccountResultCode.AccountNotFoundError,
        DeregisterUserFromAccountResultCode.AccountNotFoundError
    )]
    [InlineData(
        RemoveUserFromAccountResultCode.UserNotInAccountError,
        DeregisterUserFromAccountResultCode.UserNotInAccountError
    )]
    [InlineData(
        RemoveUserFromAccountResultCode.UserIsSoleOwnerError,
        DeregisterUserFromAccountResultCode.UserIsSoleOwnerError
    )]
    [InlineData(
        RemoveUserFromAccountResultCode.UnauthorizedError,
        DeregisterUserFromAccountResultCode.UnauthorizedError
    )]
    public void ToDeregisterUserFromAccountResultCode_MapsCorrectly(
        RemoveUserFromAccountResultCode input,
        DeregisterUserFromAccountResultCode expected
    )
    {
        var result = input.ToDeregisterUserFromAccountResultCode();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDeregisterUserFromAccountResultCode_ThrowsForUnmappedValue()
    {
        var unmappedValue = (RemoveUserFromAccountResultCode)999;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            unmappedValue.ToDeregisterUserFromAccountResultCode()
        );

        Assert.Contains("Unmapped", ex.Message);
        Assert.Contains(nameof(RemoveUserFromAccountResultCode), ex.Message);
    }

    [Fact]
    public void ToDeregisterUserFromAccountResultCode_AllEnumValuesMapped()
    {
        var allValues = Enum.GetValues<RemoveUserFromAccountResultCode>();

        foreach (var value in allValues)
        {
            var ex = Record.Exception(() => value.ToDeregisterUserFromAccountResultCode());
            Assert.Null(ex);
        }
    }
}
