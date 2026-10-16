namespace Corely.IAM.Accounts.Models;

internal record GetAccountResult(GetAccountResultCode ResultCode, string Message, Account? Account);

internal enum GetAccountResultCode
{
    Success,
    AccountNotFoundError,
    UnauthorizedError,
}
