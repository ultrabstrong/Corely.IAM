namespace Corely.IAM.Invitations.Models;

public enum AcceptInvitationResultCode
{
    Success,
    InvitationNotFoundError,
    InvitationExpiredError,
    InvitationRevokedError,
    InvitationAlreadyAcceptedError,
    UnauthorizedError,
}
