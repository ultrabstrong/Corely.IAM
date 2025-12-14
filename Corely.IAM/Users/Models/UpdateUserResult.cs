namespace Corely.IAM.Users.Models;

public record UpdateUserResult(UpdateUserResultCode ResultCode, string Message);

public enum UpdateUserResultCode
{
    Success,
    UserNotFoundError,
    UnauthorizedError,
}
