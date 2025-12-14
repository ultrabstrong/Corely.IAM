namespace Corely.IAM.Users.Models;

public record GetUserResult(GetUserResultCode ResultCode, string Message, User? User);

public enum GetUserResultCode
{
    Success,
    UserNotFoundError,
    UnauthorizedError,
}
