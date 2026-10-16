namespace Corely.IAM.Permissions.Models;

internal record DeletePermissionResult(DeletePermissionResultCode ResultCode, string Message);

internal enum DeletePermissionResultCode
{
    Success,
    PermissionNotFoundError,
    SystemDefinedPermissionError,
    UnauthorizedError,
}
