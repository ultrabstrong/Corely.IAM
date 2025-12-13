namespace Corely.IAM.Accounts.Models;

public record AddUserToAccountResult(AddUserToAccountResultCode ResultCode, string Message);

public enum AddUserToAccountResultCode
{
    Success,
    UserNotFoundError,
    AccountNotFoundError,
    UserAlreadyInAccountError,
    UnauthorizedError,
}
