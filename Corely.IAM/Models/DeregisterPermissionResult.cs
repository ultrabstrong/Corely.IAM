namespace Corely.IAM.Models;

public record DeregisterPermissionResult(DeregisterPermissionResultCode ResultCode, string Message);

public enum DeregisterPermissionResultCode
{
    Success,
    PermissionNotFoundError,
    SystemDefinedPermissionError,
    UnauthorizedError,
}
