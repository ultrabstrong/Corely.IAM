namespace Corely.IAM.EmailVerifications.Models;

public enum RejectEmailVerificationResultCode
{
    Success,
    EmailVerificationNotFoundError,
    AlreadyConfirmedError,
}

public record RejectEmailVerificationResult(
    RejectEmailVerificationResultCode ResultCode,
    string Message,
    bool Removed = false
);
