using Corely.IAM.Models;

namespace Corely.IAM.Users.Models.Extensions;

internal static class RenewUserAuthTokenResultCodeExtensions
{
    extension(RenewUserAuthTokenResultCode resultCode)
    {
        public RenewAuthTokenResult ToFailedRenewAuthTokenResult() =>
            resultCode switch
            {
                RenewUserAuthTokenResultCode.UserNotFoundError => RenewAuthTokenResult.Failed(
                    RenewAuthTokenResultCode.UserNotFoundError,
                    "User not found"
                ),
                RenewUserAuthTokenResultCode.SignatureKeyNotFoundError =>
                    RenewAuthTokenResult.Failed(
                        RenewAuthTokenResultCode.SignatureKeyNotFoundError,
                        "User signature key not found"
                    ),
                RenewUserAuthTokenResultCode.AccountNotFoundError => RenewAuthTokenResult.Failed(
                    RenewAuthTokenResultCode.AccountNotFoundError,
                    "Account not found for user"
                ),
                RenewUserAuthTokenResultCode.SessionExpiredError => RenewAuthTokenResult.Failed(
                    RenewAuthTokenResultCode.SessionExpiredError,
                    "Auth session has expired"
                ),
                RenewUserAuthTokenResultCode.TwoFactorRequiredError => RenewAuthTokenResult.Failed(
                    RenewAuthTokenResultCode.TwoFactorRequiredError,
                    "Two factor sign in is required to stay in this account as a platform member"
                ),
                RenewUserAuthTokenResultCode.RefreshTokenReusedError => RenewAuthTokenResult.Failed(
                    RenewAuthTokenResultCode.RefreshTokenReusedError,
                    "Refresh token was already used, so its session has been ended"
                ),
                RenewUserAuthTokenResultCode.RefreshTokenAlreadyRotatedError =>
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
