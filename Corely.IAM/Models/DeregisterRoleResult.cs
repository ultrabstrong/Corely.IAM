namespace Corely.IAM.Models;

public record DeregisterRoleResult(DeregisterRoleResultCode ResultCode, string Message);

public enum DeregisterRoleResultCode
{
    Success,
    RoleNotFoundError,
    SystemDefinedRoleError,
    UnauthorizedError,
}
