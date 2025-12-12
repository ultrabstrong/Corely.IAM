using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Users.Models;

public enum UserAuthTokenResultCode
{
    Success,
    UserNotFound,
    SignatureKeyNotFound,
    AccountNotFound,
}

public record UserAuthTokenResult(
    UserAuthTokenResultCode ResultCode,
    string? Token,
    List<Account> Accounts,
    int? SignedInAccountId
);
