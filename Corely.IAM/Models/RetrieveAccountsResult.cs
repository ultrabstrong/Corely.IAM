using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Models;

internal record RetrieveAccountsResult(
    RetrieveAccountsResultCode ResultCode,
    string Message,
    List<Account> Accounts
);

internal enum RetrieveAccountsResultCode
{
    Success,
    UnauthorizedError,
}
