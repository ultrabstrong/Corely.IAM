namespace Corely.IAM.Invitations.Models;

public enum InvitationSendResultCode
{
    Sent,
    NotSent,
}

public record InvitationSendResult(InvitationSendResultCode ResultCode, string? Message = null);
