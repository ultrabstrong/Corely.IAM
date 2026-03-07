namespace Corely.IAM.Roles.Models;

public enum CreateRoleResultCode
{
    Success,
    RoleExistsError,
    AccountNotFoundError,
    UnauthorizedError,
    ValidationError,
}

internal record CreateRoleResult(CreateRoleResultCode ResultCode, string Message, Guid CreatedId);
