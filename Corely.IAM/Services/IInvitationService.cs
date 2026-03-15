using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;

namespace Corely.IAM.Services;

public interface IInvitationService
{
    Task<CreateInvitationResult> CreateInvitationAsync(CreateInvitationRequest request);
    Task<AcceptInvitationResult> AcceptInvitationAsync(AcceptInvitationRequest request);
    Task<RevokeInvitationResult> RevokeInvitationAsync(Guid invitationId);
    Task<RetrieveListResult<Invitation>> ListInvitationsAsync(ListInvitationsRequest request);
}
