using Corely.IAM.Users.Models;

namespace Corely.IAM.UnitTests.Users.Models;

public class UserAuthTokenResultsTests
{
    [Fact]
    public void UserAuthTokenResult_Failed_CarriesOnlyTheCode()
    {
        var result = UserAuthTokenResult.Failed(UserAuthTokenResultCode.UserNotFoundError);

        Assert.Equal(UserAuthTokenResultCode.UserNotFoundError, result.ResultCode);
        Assert.Null(result.Token);
        Assert.Null(result.TokenId);
        Assert.Null(result.User);
        Assert.Null(result.CurrentAccount);
        Assert.Empty(result.AvailableAccounts);
    }

    [Fact]
    public void RenewUserAuthTokenResult_Failed_CarriesOnlyTheCode()
    {
        var result = RenewUserAuthTokenResult.Failed(
            RenewUserAuthTokenResultCode.SessionExpiredError
        );

        Assert.Equal(RenewUserAuthTokenResultCode.SessionExpiredError, result.ResultCode);
        Assert.Null(result.Token);
        Assert.Null(result.TokenId);
        Assert.Null(result.User);
        Assert.Null(result.CurrentAccount);
        Assert.Null(result.DeviceId);
        Assert.Empty(result.AvailableAccounts);
    }

    [Fact]
    public void UserAuthTokenValidationResult_Failed_CarriesOnlyTheCode()
    {
        var result = UserAuthTokenValidationResult.Failed(
            UserAuthTokenValidationResultCode.TokenValidationFailed
        );

        Assert.Equal(UserAuthTokenValidationResultCode.TokenValidationFailed, result.ResultCode);
        Assert.Null(result.User);
        Assert.Null(result.CurrentAccount);
        Assert.Null(result.DeviceId);
        Assert.Null(result.TokenId);
        Assert.Empty(result.AvailableAccounts);
    }
}
