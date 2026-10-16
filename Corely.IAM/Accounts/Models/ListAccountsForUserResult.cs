namespace Corely.IAM.Accounts.Models;

internal record ListAccountsForUserResult(
    ListAccountsForUserResultCode ResultCode,
    string Message,
    List<Account> Accounts
);

internal enum ListAccountsForUserResultCode
{
    Success,
    UnauthorizedError,
}
