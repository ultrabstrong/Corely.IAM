namespace Corely.IAM.EmailVerifications.Models;

public enum EmailVerificationSendResultCode
{
    Sent,
    NotSent,
}

public record EmailVerificationSendResult(
    EmailVerificationSendResultCode ResultCode,
    string? Message = null
);
