using Corely.IAM.Invitations.Models;

namespace Corely.IAM.Invitations.Providers;

/// <summary>
/// Delivers a new invitation to its recipient by whatever channel the host chooses. Optional: with
/// none registered, the token is returned to the inviter to hand over.
/// </summary>
public interface IInvitationSender
{
    Task<InvitationSendResult> SendAsync(InvitationDelivery delivery);
}
