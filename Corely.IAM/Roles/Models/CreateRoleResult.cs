namespace Corely.IAM.Roles.Models;

public enum CreateRoleResultCode
{
    Success,
    RoleExistsError,
    AccountNotFoundError,
}

internal record CreateRoleResult(CreateRoleResultCode ResultCode, string Message, int CreatedId);
