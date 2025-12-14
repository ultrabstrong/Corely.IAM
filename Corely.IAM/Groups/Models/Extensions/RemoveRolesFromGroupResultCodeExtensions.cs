using Corely.IAM.Models;

namespace Corely.IAM.Groups.Models.Extensions;

internal static class RemoveRolesFromGroupResultCodeExtensions
{
    public static DeregisterRolesFromGroupResultCode ToDeregisterRolesFromGroupResultCode(
        this RemoveRolesFromGroupResultCode resultCode
    ) =>
        resultCode switch
        {
            RemoveRolesFromGroupResultCode.Success => DeregisterRolesFromGroupResultCode.Success,
            RemoveRolesFromGroupResultCode.PartialSuccess =>
                DeregisterRolesFromGroupResultCode.PartialSuccess,
            RemoveRolesFromGroupResultCode.InvalidRoleIdsError =>
                DeregisterRolesFromGroupResultCode.InvalidRoleIdsError,
            RemoveRolesFromGroupResultCode.GroupNotFoundError =>
                DeregisterRolesFromGroupResultCode.GroupNotFoundError,
            RemoveRolesFromGroupResultCode.OwnerRoleRemovalBlockedError =>
                DeregisterRolesFromGroupResultCode.OwnerRoleRemovalBlockedError,
            RemoveRolesFromGroupResultCode.UnauthorizedError =>
                DeregisterRolesFromGroupResultCode.UnauthorizedError,
            _ => throw new ArgumentOutOfRangeException(
                nameof(resultCode),
                resultCode,
                $"Unmapped {nameof(RemoveRolesFromGroupResultCode)}: {resultCode}"
            ),
        };
}
