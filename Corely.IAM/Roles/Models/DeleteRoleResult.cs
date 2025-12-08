namespace Corely.IAM.Roles.Models;

public record DeleteRoleResult(DeleteRoleResultCode ResultCode, string Message);

public enum DeleteRoleResultCode
{
    Success,
    RoleNotFoundError,
}
