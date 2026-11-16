namespace Corely.IAM.Models;

public enum SignInResultCode
{
    Success,
    MfaRequiredChallenge,
    UserNotFoundError,
    UserLockedError,
    PasswordMismatchError,
    SignatureKeyNotFoundError,
    AccountNotFoundError,
    InvalidAuthTokenError,
    InvalidMfaCodeError,
    MfaChallengeExpiredError,
    TwoFactorRequiredError,
    InvalidApiKeyError,
    EmailVerificationRequiredChallenge,
    EmailVerificationPendingError,
    EmailVerificationChallengeExpiredError,
}

public record SignInResult(
    SignInResultCode ResultCode,
    string? Message,
    string? AuthToken,
    Guid? AuthTokenId,
    string? MfaChallengeToken = null,
    string? RefreshToken = null,
    DateTime? RefreshExpiresUtc = null,
    string? EmailVerificationChallengeToken = null
)
{
    internal static SignInResult Failed(SignInResultCode resultCode, string message) =>
        new(resultCode, message, null, null);
}
