namespace Corely.IAM.Invitations.Models;

public enum CreateInvitationResultCode
{
    Success,
    AccountNotFoundError,
    ValidationError,
    UnauthorizedError,
    UserAlreadyInAccountError,
}

public record CreateInvitationResult(
    CreateInvitationResultCode ResultCode,
    string Message,
    string? Token,
    Guid? InvitationId,
    Guid? ExistingUserId = null
);
