using Corely.IAM.Models;

namespace Corely.IAM.Permissions.Models.Extensions;

internal static class DeletePermissionResultCodeExtensions
{
    extension(DeletePermissionResultCode resultCode)
    {
        public DeregisterPermissionResultCode ToDeregisterPermissionResultCode() =>
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
}
