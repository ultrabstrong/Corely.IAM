namespace Corely.IAM.EmailVerifications.Models;

public enum GetEmailVerificationStatusResultCode
{
    Success,
    ChallengeNotFoundError,
}

public record GetEmailVerificationStatusResult(
    GetEmailVerificationStatusResultCode ResultCode,
    string Message,
    EmailVerificationStatus? Status
);
