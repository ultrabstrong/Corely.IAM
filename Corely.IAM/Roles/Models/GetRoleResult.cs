namespace Corely.IAM.Roles.Models;

internal record GetRoleResult(GetRoleResultCode ResultCode, string Message, Role? Role);

internal enum GetRoleResultCode
{
    Success,
    RoleNotFoundError,
    UnauthorizedError,
}
