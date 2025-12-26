using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Users.Models;

public enum UserAuthTokenValidationResultCode
{
    Success,
    InvalidTokenFormat,
    MissingUserIdClaim,
    MissingDeviceIdClaim,
    TokenValidationFailed,
}

internal record UserAuthTokenValidationResult(
    UserAuthTokenValidationResultCode ResultCode,
    User? User,
    Account? CurrentAccount,
    string? DeviceId,
    List<Account> AvailableAccounts
);
