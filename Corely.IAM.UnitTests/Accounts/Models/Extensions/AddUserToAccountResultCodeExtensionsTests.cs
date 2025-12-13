using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Models.Extensions;
using Corely.IAM.Models;

namespace Corely.IAM.UnitTests.Accounts.Models.Extensions;

public class AddUserToAccountResultCodeExtensionsTests
{
    [Theory]
    [InlineData(AddUserToAccountResultCode.Success, RegisterUserWithAccountResultCode.Success)]
    [InlineData(
        AddUserToAccountResultCode.UserNotFoundError,
        RegisterUserWithAccountResultCode.UserNotFoundError
    )]
    [InlineData(
        AddUserToAccountResultCode.AccountNotFoundError,
        RegisterUserWithAccountResultCode.AccountNotFoundError
    )]
    [InlineData(
        AddUserToAccountResultCode.UserAlreadyInAccountError,
        RegisterUserWithAccountResultCode.UserAlreadyInAccountError
    )]
    [InlineData(
        AddUserToAccountResultCode.UnauthorizedError,
        RegisterUserWithAccountResultCode.UnauthorizedError
    )]
    public void ToRegisterUserWithAccountResultCode_MapsCorrectly(
        AddUserToAccountResultCode input,
        RegisterUserWithAccountResultCode expected
    )
    {
        var result = input.ToRegisterUserWithAccountResultCode();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToRegisterUserWithAccountResultCode_ThrowsForUnmappedValue()
    {
        var unmappedValue = (AddUserToAccountResultCode)999;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            unmappedValue.ToRegisterUserWithAccountResultCode()
        );

        Assert.Contains("Unmapped", ex.Message);
        Assert.Contains(nameof(AddUserToAccountResultCode), ex.Message);
    }

    [Fact]
    public void ToRegisterUserWithAccountResultCode_AllEnumValuesMapped()
    {
        var allValues = Enum.GetValues<AddUserToAccountResultCode>();

        foreach (var value in allValues)
        {
            var ex = Record.Exception(() => value.ToRegisterUserWithAccountResultCode());
            Assert.Null(ex);
        }
    }
}
