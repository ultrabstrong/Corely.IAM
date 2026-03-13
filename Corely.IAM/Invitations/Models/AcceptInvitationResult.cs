namespace Corely.IAM.Invitations.Models;

public enum AcceptInvitationResultCode
{
    Success,
    InvitationNotFoundError,
    InvitationExpiredError,
    InvitationRevokedError,
    InvitationAlreadyAcceptedError,
    AddToAccountError,
    EmailMismatchError,
    UnauthorizedError,
}

public record AcceptInvitationResult(
    AcceptInvitationResultCode ResultCode,
    string Message,
    Guid? AccountId
);
