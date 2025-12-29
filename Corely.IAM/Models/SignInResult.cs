namespace Corely.IAM.Models;

public enum SignInResultCode
{
    Success,
    UserNotFoundError,
    UserLockedError,
    PasswordMismatchError,
    SignatureKeyNotFoundError,
    AccountNotFoundError,
    InvalidAuthTokenError,
}

public record SignInResult(
    SignInResultCode ResultCode,
    string? Message,
    string? AuthToken,
    Guid? AuthTokenId
);
