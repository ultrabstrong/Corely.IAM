namespace Corely.IAM.Users.Models;

internal record DeleteUserResult(DeleteUserResultCode ResultCode, string Message);

internal enum DeleteUserResultCode
{
    Success,
    UserNotFoundError,
    UserIsSoleAccountOwnerError,
    UnauthorizedError,
}
