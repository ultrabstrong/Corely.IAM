namespace Corely.IAM.Users.Models;

public enum CreateUserResultCode
{
    Success,
    UserExistsError,
    ValidationError,
}

internal record CreateUserResult(CreateUserResultCode ResultCode, string Message, Guid CreatedId);
