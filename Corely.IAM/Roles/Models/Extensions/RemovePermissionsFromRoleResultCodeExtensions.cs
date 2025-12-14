using Corely.IAM.Models;

namespace Corely.IAM.Roles.Models.Extensions;

internal static class RemovePermissionsFromRoleResultCodeExtensions
{
    public static DeregisterPermissionsFromRoleResultCode ToDeregisterPermissionsFromRoleResultCode(
        this RemovePermissionsFromRoleResultCode resultCode
    ) =>
        resultCode switch
        {
            RemovePermissionsFromRoleResultCode.Success =>
                DeregisterPermissionsFromRoleResultCode.Success,
            RemovePermissionsFromRoleResultCode.PartialSuccess =>
                DeregisterPermissionsFromRoleResultCode.PartialSuccess,
            RemovePermissionsFromRoleResultCode.InvalidPermissionIdsError =>
                DeregisterPermissionsFromRoleResultCode.InvalidPermissionIdsError,
            RemovePermissionsFromRoleResultCode.RoleNotFoundError =>
                DeregisterPermissionsFromRoleResultCode.RoleNotFoundError,
            RemovePermissionsFromRoleResultCode.SystemPermissionRemovalError =>
                DeregisterPermissionsFromRoleResultCode.SystemPermissionRemovalError,
            RemovePermissionsFromRoleResultCode.UnauthorizedError =>
                DeregisterPermissionsFromRoleResultCode.UnauthorizedError,
            _ => throw new ArgumentOutOfRangeException(
                nameof(resultCode),
                resultCode,
                $"Unmapped {nameof(RemovePermissionsFromRoleResultCode)}: {resultCode}"
            ),
        };
}
