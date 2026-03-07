namespace Corely.IAM.Invitations.Models;

public enum RevokeInvitationResultCode
{
    Success,
    InvitationNotFoundError,
    InvitationAlreadyAcceptedError,
    UnauthorizedError,
}

public record RevokeInvitationResult(RevokeInvitationResultCode ResultCode, string Message);
