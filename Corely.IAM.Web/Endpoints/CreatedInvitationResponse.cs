using Corely.IAM.Invitations.Enums;

namespace Corely.IAM.Web.Endpoints;

internal sealed record CreatedInvitationResponse(
    Guid Id,
    string? Token,
    DateTime ExpiresUtc,
    InvitationDeliveryStatus Delivery,
    string? DeliveryMessage
);
