using Corely.IAM.Models;

namespace Corely.IAM.Users.Models.Extensions;

internal static class RenewUserAuthTokenResultCodeExtensions
{
    extension(RenewUserAuthTokenResultCode resultCode)
    {
        public RenewAuthTokenResult ToFailedRenewAuthTokenResult() =>
            resultCode switch
            {
                RenewUserAuthTokenResultCode.InvalidTokenFormat => RenewAuthTokenResult.Failed(
                    RenewAuthTokenResultCode.InvalidTokenFormat,
                    "Auth token is in an invalid format"
                ),
                RenewUserAuthTokenResultCode.MissingUserIdClaim => RenewAuthTokenResult.Failed(
                    RenewAuthTokenResultCode.MissingUserIdClaim,
                    "User ID claim is missing"
                ),
                RenewUserAuthTokenResultCode.MissingDeviceIdClaim => RenewAuthTokenResult.Failed(
                    RenewAuthTokenResultCode.MissingDeviceIdClaim,
                    "Device ID claim is missing"
                ),
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
                _ => RenewAuthTokenResult.Failed(
                    RenewAuthTokenResultCode.InvalidAuthTokenError,
                    "Auth token is invalid"
                ),
            };
    }
}
