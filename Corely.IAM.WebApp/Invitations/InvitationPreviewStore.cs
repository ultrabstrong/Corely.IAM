using Corely.IAM.Invitations.Models;

namespace Corely.IAM.WebApp.Invitations;

public sealed class InvitationPreviewStore
{
    public InvitationDelivery? Last { get; set; }
}
