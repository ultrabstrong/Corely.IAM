using Corely.IAM.Models;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Models.Extensions;

namespace Corely.IAM.UnitTests.Users.Models.Extensions;

public class RenewUserAuthTokenResultCodeExtensionsTests
{
    [Theory]
    [InlineData(
        RenewUserAuthTokenResultCode.InvalidTokenFormat,
        RenewAuthTokenResultCode.InvalidTokenFormat
    )]
    [InlineData(
        RenewUserAuthTokenResultCode.MissingUserIdClaim,
        RenewAuthTokenResultCode.MissingUserIdClaim
    )]
    [InlineData(
        RenewUserAuthTokenResultCode.MissingDeviceIdClaim,
        RenewAuthTokenResultCode.MissingDeviceIdClaim
    )]
    [InlineData(
        RenewUserAuthTokenResultCode.UserNotFoundError,
        RenewAuthTokenResultCode.UserNotFoundError
    )]
    [InlineData(
        RenewUserAuthTokenResultCode.SignatureKeyNotFoundError,
        RenewAuthTokenResultCode.SignatureKeyNotFoundError
    )]
    [InlineData(
        RenewUserAuthTokenResultCode.AccountNotFoundError,
        RenewAuthTokenResultCode.AccountNotFoundError
    )]
    [InlineData(
        RenewUserAuthTokenResultCode.SessionExpiredError,
        RenewAuthTokenResultCode.SessionExpiredError
    )]
    [InlineData(
        RenewUserAuthTokenResultCode.TokenValidationFailed,
        RenewAuthTokenResultCode.InvalidAuthTokenError
    )]
    public void ToFailedRenewAuthTokenResult_MapsTheCode(
        object code,
        RenewAuthTokenResultCode expected
    )
    {
        var result = ((RenewUserAuthTokenResultCode)code).ToFailedRenewAuthTokenResult();

        Assert.Equal(expected, result.ResultCode);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
        Assert.Null(result.AuthToken);
        Assert.Null(result.AuthTokenId);
    }
}
