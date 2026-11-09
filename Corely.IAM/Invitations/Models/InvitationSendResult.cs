namespace Corely.IAM.Invitations.Models;

public enum InvitationSendResultCode
{
    Sent,
    NotSent,
}

/// <param name="Message">Shown to the inviter in place of the default text. For
/// <see cref="InvitationSendResultCode.NotSent"/>, the reason, shown beside the link.</param>
public record InvitationSendResult(InvitationSendResultCode ResultCode, string? Message = null);
