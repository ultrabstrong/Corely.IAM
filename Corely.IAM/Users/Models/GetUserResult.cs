namespace Corely.IAM.Users.Models;

internal record GetUserResult(GetUserResultCode ResultCode, string Message, User? User);

internal enum GetUserResultCode
{
    Success,
    UserNotFoundError,
    UnauthorizedError,
}
