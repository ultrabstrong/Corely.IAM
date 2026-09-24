using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Users.Models;

public enum UserAuthTokenResultCode
{
    Success,
    UserNotFoundError,
    SignatureKeyNotFoundError,
    AccountNotFoundError,
}

public record UserAuthTokenResult(
    UserAuthTokenResultCode ResultCode,
    string? Token,
    Guid? TokenId,
    User? User,
    Account? CurrentAccount,
    List<Account> AvailableAccounts
)
{
    internal static UserAuthTokenResult Failed(UserAuthTokenResultCode resultCode) =>
        new(resultCode, null, null, null, null, []);
}
