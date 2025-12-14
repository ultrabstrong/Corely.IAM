using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Models;

public record RetrieveAccountsResult(
    RetrieveAccountsResultCode ResultCode,
    string Message,
    List<Account> Accounts
);

public enum RetrieveAccountsResultCode
{
    Success,
    UnauthorizedError,
}
