namespace Corely.IAM.Models;

public enum RegisterUserWithAccountResultCode
{
    Success,
    UserNotFoundError,
    AccountNotFoundError,
    UserAlreadyInAccountError,
}

public record RegisterUserWithAccountResult(
    RegisterUserWithAccountResultCode ResultCode,
    string? Message
);
