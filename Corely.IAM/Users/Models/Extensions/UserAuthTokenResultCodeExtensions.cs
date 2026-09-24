using Corely.IAM.Models;

namespace Corely.IAM.Users.Models.Extensions;

internal static class UserAuthTokenResultCodeExtensions
{
    extension(UserAuthTokenResultCode resultCode)
    {
        public SignInResult ToFailedSignInResult(Guid? accountId) =>
            resultCode switch
            {
                UserAuthTokenResultCode.UserNotFoundError => SignInResult.Failed(
                    SignInResultCode.UserNotFoundError,
                    "User not found"
                ),
                UserAuthTokenResultCode.SignatureKeyNotFoundError => SignInResult.Failed(
                    SignInResultCode.SignatureKeyNotFoundError,
                    "User signature key not found"
                ),
                UserAuthTokenResultCode.AccountNotFoundError => SignInResult.Failed(
                    SignInResultCode.AccountNotFoundError,
                    $"Account {accountId} not found for user"
                ),
                _ => SignInResult.Failed(
                    SignInResultCode.UserNotFoundError,
                    "Unknown error creating auth token"
                ),
            };
    }
}
