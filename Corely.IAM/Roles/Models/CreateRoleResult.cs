namespace Corely.IAM.Roles.Models;

public enum CreateRoleResultCode
{
    Success,
    RoleExistsError,
    AccountNotFoundError,
    UnauthorizedError,
}

internal record CreateRoleResult(CreateRoleResultCode ResultCode, string Message, int CreatedId);
