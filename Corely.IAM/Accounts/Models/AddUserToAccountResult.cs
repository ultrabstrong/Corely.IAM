namespace Corely.IAM.Accounts.Models;

internal record AddUserToAccountResult(AddUserToAccountResultCode ResultCode, string Message);

internal enum AddUserToAccountResultCode
{
    Success,
    UserNotFoundError,
    AccountNotFoundError,
    UserAlreadyInAccountError,
    UnauthorizedError,
}
