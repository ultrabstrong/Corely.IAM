namespace Corely.IAM.Invitations.Models;

public enum CreateInvitationResultCode
{
    Success,
    AccountNotFoundError,
    ValidationError,
    UnauthorizedError,
    UserAlreadyInAccountError,
}
