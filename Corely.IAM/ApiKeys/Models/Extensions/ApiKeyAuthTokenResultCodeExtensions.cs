using Corely.IAM.Models;

namespace Corely.IAM.ApiKeys.Models.Extensions;

internal static class ApiKeyAuthTokenResultCodeExtensions
{
    extension(ApiKeyAuthTokenResultCode resultCode)
    {
        public RenewAuthTokenResult ToFailedRenewAuthTokenResult() =>
            resultCode switch
            {
                ApiKeyAuthTokenResultCode.SessionExpiredError => RenewAuthTokenResult.Failed(
                    RenewAuthTokenResultCode.SessionExpiredError,
                    "Auth session has expired"
                ),
                ApiKeyAuthTokenResultCode.RefreshTokenReusedError => RenewAuthTokenResult.Failed(
                    RenewAuthTokenResultCode.RefreshTokenReusedError,
                    "Refresh token was already used, so its session has been ended"
                ),
                ApiKeyAuthTokenResultCode.RefreshTokenAlreadyRotatedError =>
                    RenewAuthTokenResult.Failed(
                        RenewAuthTokenResultCode.RefreshTokenAlreadyRotatedError,
                        "Refresh token was just replaced by another renewal"
                    ),
                _ => RenewAuthTokenResult.Failed(
                    RenewAuthTokenResultCode.InvalidAuthTokenError,
                    "Auth token is invalid"
                ),
            };
    }
}
