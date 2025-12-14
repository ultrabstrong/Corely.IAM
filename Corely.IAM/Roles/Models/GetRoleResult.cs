namespace Corely.IAM.Roles.Models;

public record GetRoleResult(GetRoleResultCode ResultCode, string Message, Role? Role);

public enum GetRoleResultCode
{
    Success,
    RoleNotFoundError,
    UnauthorizedError,
}
