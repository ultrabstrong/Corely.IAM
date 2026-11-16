namespace Corely.IAM.Models;

public enum RegisterUserWithAccountResultCode
{
    Success,
    UserNotFoundError,
    AccountNotFoundError,
    UserAlreadyInAccountError,
    UnauthorizedError,
    EmailNotVerifiedError,
}

public record RegisterUserWithAccountResult(
    RegisterUserWithAccountResultCode ResultCode,
    string Message
);
