namespace Corely.IAM.Models;

public enum RenewAuthTokenResultCode
{
    Success,
    InvalidAuthTokenError,
    UserNotFoundError,
    SignatureKeyNotFoundError,
    AccountNotFoundError,
    SessionExpiredError,
    TwoFactorRequiredError,
    RefreshTokenReusedError,
    RefreshTokenAlreadyRotatedError,
}

public record RenewAuthTokenResult(
    RenewAuthTokenResultCode ResultCode,
    string? Message,
    string? AuthToken,
    Guid? AuthTokenId,
    string? RefreshToken,
    DateTime? RefreshExpiresUtc
)
{
    internal static RenewAuthTokenResult Failed(
        RenewAuthTokenResultCode resultCode,
        string message
    ) => new(resultCode, message, null, null, null, null);
}
