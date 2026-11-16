namespace Corely.IAM.EmailVerifications.Models;

public record EmailVerificationDelivery(
    Guid VerificationId,
    Guid UserId,
    string Username,
    string Email,
    DateTime ExpiresUtc,
    string Token
);
