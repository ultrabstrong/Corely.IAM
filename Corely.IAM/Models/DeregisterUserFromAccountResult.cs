namespace Corely.IAM.Models;

public record DeregisterUserFromAccountResult(
    DeregisterUserFromAccountResultCode ResultCode,
    string Message
);

public enum DeregisterUserFromAccountResultCode
{
    Success,
    UserNotFoundError,
    AccountNotFoundError,
    UserNotInAccountError,
    UserIsSoleOwnerError,
    UnauthorizedError,
}
