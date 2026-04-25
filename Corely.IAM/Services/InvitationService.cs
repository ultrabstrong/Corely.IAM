using Corely.Common.Extensions;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Invitations.Processors;
using Corely.IAM.Models;

namespace Corely.IAM.Services;

internal class InvitationService(IInvitationProcessor invitationProcessor) : IInvitationService
{
    private readonly IInvitationProcessor _invitationProcessor = invitationProcessor.ThrowIfNull(
        nameof(invitationProcessor)
    );

    public Task<CreateInvitationResult> CreateInvitationAsync(CreateInvitationRequest request) =>
        _invitationProcessor.CreateInvitationAsync(request);

    public Task<AcceptInvitationResult> AcceptInvitationAsync(AcceptInvitationRequest request) =>
        _invitationProcessor.AcceptInvitationAsync(request);

    public Task<RevokeInvitationResult> RevokeInvitationAsync(RevokeInvitationRequest request) =>
        _invitationProcessor.RevokeInvitationAsync(request.InvitationId);

    public async Task<RetrieveListResult<Invitation>> ListInvitationsAsync(
        ListInvitationsRequest request
    )
    {
        var result = await _invitationProcessor.ListInvitationsAsync(request);
        return new RetrieveListResult<Invitation>(result.ResultCode, result.Message, result.Data);
    }
}
