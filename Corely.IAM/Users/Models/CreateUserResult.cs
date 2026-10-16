namespace Corely.IAM.Users.Models;

internal enum CreateUserResultCode
{
    Success,
    UserExistsError,
    ValidationError,
}

internal record CreateUserResult(CreateUserResultCode ResultCode, string Message, Guid CreatedId);
