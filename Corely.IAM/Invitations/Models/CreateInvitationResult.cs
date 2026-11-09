using Corely.IAM.Invitations.Enums;

namespace Corely.IAM.Invitations.Models;

public enum CreateInvitationResultCode
{
    Success,
    AccountNotFoundError,
    ValidationError,
    UnauthorizedError,
    UserAlreadyInAccountError,
}

/// <param name="Token">The token to hand to the recipient. Null when the host's sender delivered
/// it (<see cref="Delivery"/> is <see cref="InvitationDeliveryStatus.Sent"/>).</param>
/// <param name="DeliveryMessage">What to tell the inviter about delivery: the sender's message, or
/// a default when it sent without one.</param>
public record CreateInvitationResult(
    CreateInvitationResultCode ResultCode,
    string Message,
    string? Token,
    Guid? InvitationId,
    Guid? ExistingUserId = null,
    DateTime? ExpiresUtc = null,
    InvitationDeliveryStatus Delivery = InvitationDeliveryStatus.NotAttempted,
    string? DeliveryMessage = null
);
