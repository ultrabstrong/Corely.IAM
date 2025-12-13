using Corely.IAM.Models;

namespace Corely.IAM.Users.Models.Extensions;

internal static class DeleteUserResultCodeExtensions
{
    public static DeregisterUserResultCode ToDeregisterUserResultCode(
        this DeleteUserResultCode resultCode
    ) =>
        resultCode switch
        {
            DeleteUserResultCode.Success => DeregisterUserResultCode.Success,
            DeleteUserResultCode.UserNotFoundError => DeregisterUserResultCode.UserNotFoundError,
            DeleteUserResultCode.UserIsSoleAccountOwnerError =>
                DeregisterUserResultCode.UserIsSoleAccountOwnerError,
            DeleteUserResultCode.UnauthorizedError => DeregisterUserResultCode.UnauthorizedError,
            _ => throw new ArgumentOutOfRangeException(
                nameof(resultCode),
                resultCode,
                $"Unmapped {nameof(DeleteUserResultCode)}: {resultCode}"
            ),
        };
}
