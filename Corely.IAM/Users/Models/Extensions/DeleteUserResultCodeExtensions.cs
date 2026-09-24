using Corely.IAM.Models;

namespace Corely.IAM.Users.Models.Extensions;

internal static class DeleteUserResultCodeExtensions
{
    extension(DeleteUserResultCode resultCode)
    {
        public DeregisterUserResultCode ToDeregisterUserResultCode() =>
            resultCode switch
            {
                DeleteUserResultCode.Success => DeregisterUserResultCode.Success,
                DeleteUserResultCode.UserNotFoundError =>
                    DeregisterUserResultCode.UserNotFoundError,
                DeleteUserResultCode.UserIsSoleAccountOwnerError =>
                    DeregisterUserResultCode.UserIsSoleAccountOwnerError,
                DeleteUserResultCode.UnauthorizedError =>
                    DeregisterUserResultCode.UnauthorizedError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(DeleteUserResultCode)}: {resultCode}"
                ),
            };
    }
}
