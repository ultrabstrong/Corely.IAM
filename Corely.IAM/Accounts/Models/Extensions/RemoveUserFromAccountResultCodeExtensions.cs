using Corely.IAM.Models;

namespace Corely.IAM.Accounts.Models.Extensions;

internal static class RemoveUserFromAccountResultCodeExtensions
{
    extension(RemoveUserFromAccountResultCode resultCode)
    {
        public DeregisterUserFromAccountResultCode ToDeregisterUserFromAccountResultCode() =>
            resultCode switch
            {
                RemoveUserFromAccountResultCode.Success =>
                    DeregisterUserFromAccountResultCode.Success,
                RemoveUserFromAccountResultCode.UserNotFoundError =>
                    DeregisterUserFromAccountResultCode.UserNotFoundError,
                RemoveUserFromAccountResultCode.AccountNotFoundError =>
                    DeregisterUserFromAccountResultCode.AccountNotFoundError,
                RemoveUserFromAccountResultCode.UserNotInAccountError =>
                    DeregisterUserFromAccountResultCode.UserNotInAccountError,
                RemoveUserFromAccountResultCode.UserIsSoleOwnerError =>
                    DeregisterUserFromAccountResultCode.UserIsSoleOwnerError,
                RemoveUserFromAccountResultCode.UnauthorizedError =>
                    DeregisterUserFromAccountResultCode.UnauthorizedError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(RemoveUserFromAccountResultCode)}: {resultCode}"
                ),
            };
    }
}
