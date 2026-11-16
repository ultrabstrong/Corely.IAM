namespace Corely.IAM.EmailVerifications.Models;

public enum EmailVerificationState
{
    NotSent,
    Sent,
    Expired,
    Confirmed,
}

public record EmailVerificationStatus(
    string Email,
    EmailVerificationState State,
    DateTime? SentUtc,
    DateTime? ExpiresUtc,
    DateTime? NextResendUtc
);
