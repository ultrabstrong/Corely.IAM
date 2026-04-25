namespace Corely.IAM.Invitations.Models;

public record RevokeInvitationRequest(Guid AccountId, Guid InvitationId);
