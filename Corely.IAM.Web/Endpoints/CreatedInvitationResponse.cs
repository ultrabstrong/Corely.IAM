using Corely.IAM.Invitations.Enums;

namespace Corely.IAM.Web.Endpoints;

/// <param name="Token">Null when the host's sender delivered the invitation.</param>
internal sealed record CreatedInvitationResponse(
    Guid Id,
    string? Token,
    DateTime ExpiresUtc,
    InvitationDeliveryStatus Delivery,
    string? DeliveryMessage
);
