using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Users.Models;

internal enum UserAuthTokenResultCode
{
    Success,
    UserNotFoundError,
    SignatureKeyNotFoundError,
    AccountNotFoundError,
    TwoFactorRequiredError,
}

internal record UserAuthTokenResult(
    UserAuthTokenResultCode ResultCode,
    string? Token,
    Guid? TokenId,
    User? User,
    Account? CurrentAccount,
    List<Account> AvailableAccounts,
    bool EnteredAsPlatformMember = false
)
{
    internal static UserAuthTokenResult Failed(UserAuthTokenResultCode resultCode) =>
        new(resultCode, null, null, null, null, []);
}
