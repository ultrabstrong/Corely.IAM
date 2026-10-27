using Corely.IAM.Models;
using Corely.IAM.Web.Endpoints;

namespace Corely.IAM.Web.Extensions;

internal static class RenewAuthTokenResultExtensions
{
    extension(RenewAuthTokenResult result)
    {
        public TokenPairResponse? ToTokenPairResponse() =>
            result.ResultCode == RenewAuthTokenResultCode.Success
            && result.AuthToken is { } authToken
            && result.RefreshToken is { } refreshToken
            && result.RefreshExpiresUtc is { } refreshExpiresUtc
                ? new TokenPairResponse(authToken, refreshToken, refreshExpiresUtc)
                : null;
    }
}
