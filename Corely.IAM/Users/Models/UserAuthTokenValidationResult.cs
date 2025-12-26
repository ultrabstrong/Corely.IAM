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

public record UserAuthTokenValidationResult(
    UserAuthTokenValidationResultCode ResultCode,
    int? UserId,
    int? SignedInAccountId,
    string? DeviceId,
    List<Account> Accounts
);
