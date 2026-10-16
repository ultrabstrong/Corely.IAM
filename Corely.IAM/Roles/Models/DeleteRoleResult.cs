namespace Corely.IAM.Roles.Models;

internal record DeleteRoleResult(DeleteRoleResultCode ResultCode, string Message);

internal enum DeleteRoleResultCode
{
    Success,
    RoleNotFoundError,
    SystemDefinedRoleError,
    UnauthorizedError,
}
