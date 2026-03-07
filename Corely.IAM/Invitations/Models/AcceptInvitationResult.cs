namespace Corely.IAM.Invitations.Models;

public record AcceptInvitationResult(
    AcceptInvitationResultCode ResultCode,
    string Message,
    Guid? AccountId
);
