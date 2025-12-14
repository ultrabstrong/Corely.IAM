namespace Corely.IAM.Users.Models;

public enum UserAuthTokenValidationResultCode
{
    Success,
    InvalidTokenFormat,
    MissingUserIdClaim,
    TokenValidationFailed,
}

public record UserAuthTokenValidationResult(
    UserAuthTokenValidationResultCode ResultCode,
    int? UserId,
    int? SignedInAccountId
);
