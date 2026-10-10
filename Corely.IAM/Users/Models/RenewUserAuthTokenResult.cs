using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Users.Models;

internal enum RenewUserAuthTokenResultCode
{
    Success,
    InvalidTokenFormat,
    MissingUserIdClaim,
    MissingDeviceIdClaim,
    TokenValidationFailed,
    UserNotFoundError,
    SignatureKeyNotFoundError,
    AccountNotFoundError,
    SessionExpiredError,
    TwoFactorRequiredError,
}

internal record RenewUserAuthTokenResult(
    RenewUserAuthTokenResultCode ResultCode,
    string? Token,
    Guid? TokenId,
    User? User,
    Account? CurrentAccount,
    string? DeviceId,
    List<Account> AvailableAccounts,
    bool EnteredAsPlatformMember = false
)
{
    internal static RenewUserAuthTokenResult Failed(RenewUserAuthTokenResultCode resultCode) =>
        new(resultCode, null, null, null, null, null, []);
}
