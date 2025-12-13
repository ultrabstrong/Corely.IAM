using Corely.IAM.Models;

namespace Corely.IAM.Groups.Models.Extensions;

internal static class RemoveUsersFromGroupResultCodeExtensions
{
    public static DeregisterUsersFromGroupResultCode ToDeregisterUsersFromGroupResultCode(
        this RemoveUsersFromGroupResultCode resultCode
    ) =>
        resultCode switch
        {
            RemoveUsersFromGroupResultCode.Success => DeregisterUsersFromGroupResultCode.Success,
            RemoveUsersFromGroupResultCode.PartialSuccess =>
                DeregisterUsersFromGroupResultCode.PartialSuccess,
            RemoveUsersFromGroupResultCode.GroupNotFoundError =>
                DeregisterUsersFromGroupResultCode.GroupNotFoundError,
            RemoveUsersFromGroupResultCode.UserIsSoleOwnerError =>
                DeregisterUsersFromGroupResultCode.UserIsSoleOwnerError,
            RemoveUsersFromGroupResultCode.UnauthorizedError =>
                DeregisterUsersFromGroupResultCode.UnauthorizedError,
            _ => throw new ArgumentOutOfRangeException(
                nameof(resultCode),
                resultCode,
                $"Unmapped {nameof(RemoveUsersFromGroupResultCode)}: {resultCode}"
            ),
        };
}
