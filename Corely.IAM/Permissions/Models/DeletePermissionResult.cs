namespace Corely.IAM.Permissions.Models;

public record DeletePermissionResult(DeletePermissionResultCode ResultCode, string Message);

public enum DeletePermissionResultCode
{
    Success,
    PermissionNotFoundError,
}
