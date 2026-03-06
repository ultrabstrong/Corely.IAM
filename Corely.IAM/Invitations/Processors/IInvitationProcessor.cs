using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;

namespace Corely.IAM.Invitations.Processors;

internal interface IInvitationProcessor
{
    Task<CreateInvitationResult> CreateInvitationAsync(CreateInvitationRequest request);
    Task<AcceptInvitationResult> AcceptInvitationAsync(AcceptInvitationRequest request);
    Task<RevokeInvitationResult> RevokeInvitationAsync(Guid invitationId);
    Task<ListResult<Invitation>> ListInvitationsAsync(Guid accountId, int skip, int take);
}
