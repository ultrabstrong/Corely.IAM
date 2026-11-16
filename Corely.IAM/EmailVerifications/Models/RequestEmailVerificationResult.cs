namespace Corely.IAM.EmailVerifications.Models;

public enum RequestEmailVerificationResultCode
{
    Success,
    UserNotFoundError,
    AlreadyVerifiedError,
    ResendCooldownError,
    ChallengeNotFoundError,
    UnauthorizedError,
}

public record RequestEmailVerificationResult(
    RequestEmailVerificationResultCode ResultCode,
    string Message,
    string? Token = null,
    DateTime? ExpiresUtc = null,
    DateTime? NextResendUtc = null,
    EmailVerificationDeliveryStatus? Delivery = null,
    string? DeliveryMessage = null
);
