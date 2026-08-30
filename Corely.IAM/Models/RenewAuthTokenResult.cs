namespace Corely.IAM.Models;

public enum RenewAuthTokenResultCode
{
    Success,
    InvalidTokenFormat,
    MissingUserIdClaim,
    MissingDeviceIdClaim,
    InvalidAuthTokenError,
    UserNotFoundError,
    SignatureKeyNotFoundError,
    AccountNotFoundError,
    SessionExpiredError,
}

public record RenewAuthTokenResult(
    RenewAuthTokenResultCode ResultCode,
    string? Message,
    string? AuthToken,
    Guid? AuthTokenId
);
