namespace Corely.IAM.Models;

public record DeregisterUserResult(DeregisterUserResultCode ResultCode, string Message);

public enum DeregisterUserResultCode
{
    Success,
    UserNotFoundError,
    UserIsSoleAccountOwnerError,
}
