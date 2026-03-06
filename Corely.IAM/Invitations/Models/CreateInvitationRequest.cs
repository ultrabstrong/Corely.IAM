namespace Corely.IAM.Invitations.Models;

public record CreateInvitationRequest(
    Guid AccountId,
    string Email,
    string? Description,
    int ExpiresInSeconds
);
