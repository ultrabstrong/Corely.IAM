namespace Corely.IAM.Users.Models;

public enum CreateUserResultCode
{
    Success,
    UserExistsError,
}

internal record CreateUserResult(CreateUserResultCode ResultCode, string Message, int CreatedId);
