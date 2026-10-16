namespace Corely.IAM.Accounts.Models;

internal record RemoveUserFromAccountResult(
    RemoveUserFromAccountResultCode ResultCode,
    string Message
);

internal enum RemoveUserFromAccountResultCode
{
    Success,
    UserNotFoundError,
    AccountNotFoundError,
    UserNotInAccountError,
    UserIsSoleOwnerError,
    UnauthorizedError,
}
