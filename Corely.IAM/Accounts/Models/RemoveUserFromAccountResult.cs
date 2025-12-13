namespace Corely.IAM.Accounts.Models;

public record RemoveUserFromAccountResult(
    RemoveUserFromAccountResultCode ResultCode,
    string Message
);

public enum RemoveUserFromAccountResultCode
{
    Success,
    UserNotFoundError,
    AccountNotFoundError,
    UserNotInAccountError,
    UserIsSoleOwnerError,
    UnauthorizedError,
}
