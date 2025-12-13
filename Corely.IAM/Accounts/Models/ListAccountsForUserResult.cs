namespace Corely.IAM.Accounts.Models;

public record ListAccountsForUserResult(
    ListAccountsForUserResultCode ResultCode,
    string Message,
    List<Account> Accounts
);

public enum ListAccountsForUserResultCode
{
    Success,
    UnauthorizedError,
}
