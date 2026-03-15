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
    GoogleAuthNotLinkedError,
    InvalidGoogleTokenError,
    InvalidMfaCodeError,
    MfaChallengeExpiredError,
}

public record SignInResult(
    SignInResultCode ResultCode,
    string? Message,
    string? AuthToken,
    Guid? AuthTokenId,
    string? MfaChallengeToken = null
);
