namespace Corely.IAM.EmailVerifications.Models;

public enum VerifyEmailResultCode
{
    Success,
    EmailVerificationNotFoundError,
    EmailVerificationExpiredError,
    EmailVerificationAlreadyUsedError,
    EmailVerificationInvalidatedError,
    EmailChangedError,
}

public record VerifyEmailResult(
    VerifyEmailResultCode ResultCode,
    string Message,
    string? Email = null
);
