namespace Corely.IAM.Invitations.Models;

public record InvitationDelivery(
    Guid InvitationId,
    Guid AccountId,
    string AccountName,
    string Email,
    string? Description,
    DateTime ExpiresUtc,
    string InvitedByUsername,
    string InvitedByEmail,
    string Token
);
