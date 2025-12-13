using Corely.IAM.Models;

namespace Corely.IAM.Permissions.Models.Extensions;

internal static class DeletePermissionResultCodeExtensions
{
    public static DeregisterPermissionResultCode ToDeregisterPermissionResultCode(
        this DeletePermissionResultCode resultCode
    ) =>
        resultCode switch
        {
            DeletePermissionResultCode.Success => DeregisterPermissionResultCode.Success,
            DeletePermissionResultCode.PermissionNotFoundError =>
                DeregisterPermissionResultCode.PermissionNotFoundError,
            DeletePermissionResultCode.SystemDefinedPermissionError =>
                DeregisterPermissionResultCode.SystemDefinedPermissionError,
            DeletePermissionResultCode.UnauthorizedError =>
                DeregisterPermissionResultCode.UnauthorizedError,
            _ => throw new ArgumentOutOfRangeException(
                nameof(resultCode),
                resultCode,
                $"Unmapped {nameof(DeletePermissionResultCode)}: {resultCode}"
            ),
        };
}
