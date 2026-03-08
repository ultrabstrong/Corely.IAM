namespace Corely.IAM.Invitations.Models;

public record CreateInvitationResult(
    CreateInvitationResultCode ResultCode,
    string Message,
    string? Token,
    Guid? InvitationId,
    Guid? ExistingUserId = null
);
