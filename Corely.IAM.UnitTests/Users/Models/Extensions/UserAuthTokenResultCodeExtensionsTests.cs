using Corely.IAM.Models;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Models.Extensions;

namespace Corely.IAM.UnitTests.Users.Models.Extensions;

public class UserAuthTokenResultCodeExtensionsTests
{
    [Theory]
    [InlineData(UserAuthTokenResultCode.UserNotFoundError, SignInResultCode.UserNotFoundError)]
    [InlineData(
        UserAuthTokenResultCode.SignatureKeyNotFoundError,
        SignInResultCode.SignatureKeyNotFoundError
    )]
    [InlineData(
        UserAuthTokenResultCode.AccountNotFoundError,
        SignInResultCode.AccountNotFoundError
    )]
    [InlineData(UserAuthTokenResultCode.Success, SignInResultCode.UserNotFoundError)]
    public void ToFailedSignInResult_MapsTheCode(
        UserAuthTokenResultCode code,
        SignInResultCode expected
    )
    {
        var result = code.ToFailedSignInResult(null);

        Assert.Equal(expected, result.ResultCode);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
        Assert.Null(result.AuthToken);
        Assert.Null(result.AuthTokenId);
    }

    [Fact]
    public void ToFailedSignInResult_NamesTheMissingAccount()
    {
        var accountId = Guid.CreateVersion7();

        var result = UserAuthTokenResultCode.AccountNotFoundError.ToFailedSignInResult(accountId);

        Assert.Contains(accountId.ToString(), result.Message);
    }
}
