namespace Corely.IAM.Accounts.Models;

public record GetAccountResult(GetAccountResultCode ResultCode, string Message, Account? Account);

public enum GetAccountResultCode
{
    Success,
    AccountNotFoundError,
    UnauthorizedError,
}
