using Corely.IAM.Models;

namespace Corely.IAM.Users.Models.Extensions;

internal static class RemoveRolesFromUserResultCodeExtensions
{
    public static DeregisterRolesFromUserResultCode ToDeregisterRolesFromUserResultCode(
        this RemoveRolesFromUserResultCode resultCode
    ) =>
        resultCode switch
        {
            RemoveRolesFromUserResultCode.Success => DeregisterRolesFromUserResultCode.Success,
            RemoveRolesFromUserResultCode.PartialSuccess =>
                DeregisterRolesFromUserResultCode.PartialSuccess,
            RemoveRolesFromUserResultCode.InvalidRoleIdsError =>
                DeregisterRolesFromUserResultCode.InvalidRoleIdsError,
            RemoveRolesFromUserResultCode.UserNotFoundError =>
                DeregisterRolesFromUserResultCode.UserNotFoundError,
            RemoveRolesFromUserResultCode.UserIsSoleOwnerError =>
                DeregisterRolesFromUserResultCode.UserIsSoleOwnerError,
            RemoveRolesFromUserResultCode.UnauthorizedError =>
                DeregisterRolesFromUserResultCode.UnauthorizedError,
            _ => throw new ArgumentOutOfRangeException(
                nameof(resultCode),
                resultCode,
                $"Unmapped {nameof(RemoveRolesFromUserResultCode)}: {resultCode}"
            ),
        };
}
