namespace Corely.IAM.Invitations.Models;

public enum RevokeInvitationResultCode
{
    Success,
    InvitationNotFoundError,
    InvitationAlreadyAcceptedError,
    UnauthorizedError,
}
