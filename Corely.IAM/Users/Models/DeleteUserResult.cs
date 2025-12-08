namespace Corely.IAM.Users.Models;

public record DeleteUserResult(DeleteUserResultCode ResultCode, string Message);

public enum DeleteUserResultCode
{
    Success,
    UserNotFoundError,
    UserIsSoleAccountOwnerError,
}
