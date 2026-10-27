using Corely.IAM.Models;
using Corely.IAM.Web.Extensions;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class TokenPairResponseExtensionsTests
{
    private static readonly DateTime _refreshExpiresUtc = new(
        2026,
        1,
        2,
        0,
        0,
        0,
        DateTimeKind.Utc
    );

    [Fact]
    public void SignInResult_ToTokenPairResponse_CarriesThePair_OnSuccess()
    {
        var pair = new SignInResult(
            SignInResultCode.Success,
            null,
            "access",
            Guid.CreateVersion7(),
            RefreshToken: "refresh",
            RefreshExpiresUtc: _refreshExpiresUtc
        ).ToTokenPairResponse();

        Assert.NotNull(pair);
        Assert.Equal("access", pair.AuthToken);
        Assert.Equal("refresh", pair.RefreshToken);
        Assert.Equal(_refreshExpiresUtc, pair.RefreshExpiresUtc);
    }

    [Fact]
    public void SignInResult_ToTokenPairResponse_IsNull_OnFailureOrAMissingToken()
    {
        Assert.Null(
            new SignInResult(
                SignInResultCode.InvalidApiKeyError,
                null,
                null,
                null
            ).ToTokenPairResponse()
        );
        Assert.Null(
            new SignInResult(
                SignInResultCode.Success,
                null,
                "access",
                Guid.CreateVersion7()
            ).ToTokenPairResponse()
        );
    }

    [Fact]
    public void RenewAuthTokenResult_ToTokenPairResponse_CarriesThePair_OnSuccess_Only()
    {
        Assert.NotNull(
            new RenewAuthTokenResult(
                RenewAuthTokenResultCode.Success,
                null,
                "access",
                Guid.CreateVersion7(),
                "refresh",
                _refreshExpiresUtc
            ).ToTokenPairResponse()
        );
        Assert.Null(
            new RenewAuthTokenResult(
                RenewAuthTokenResultCode.RefreshTokenAlreadyRotatedError,
                null,
                "access",
                Guid.CreateVersion7(),
                "refresh",
                _refreshExpiresUtc
            ).ToTokenPairResponse()
        );
    }
}
