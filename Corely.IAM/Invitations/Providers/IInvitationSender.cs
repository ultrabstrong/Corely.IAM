using Corely.IAM.Invitations.Models;

namespace Corely.IAM.Invitations.Providers;

public interface IInvitationSender
{
    Task<InvitationSendResult> SendAsync(InvitationDelivery delivery);
}
