using Corely.Common.Extensions;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Services;

internal class InvitationServiceAuthorizationDecorator(
    IInvitationService inner,
    IAuthorizationProvider authorizationProvider
) : IInvitationService
{
    private readonly IInvitationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<CreateInvitationResult> CreateInvitationAsync(
        CreateInvitationRequest request
    ) =>
        _authorizationProvider.HasAccountContext(request.AccountId)
            ? await _inner.CreateInvitationAsync(request)
            : new CreateInvitationResult(
                CreateInvitationResultCode.UnauthorizedError,
                "Unauthorized to create invitation",
                null,
                null
            );

    public async Task<AcceptInvitationResult> AcceptInvitationAsync(
        AcceptInvitationRequest request
    ) =>
        _authorizationProvider.IsNonSystemUserContext()
            ? await _inner.AcceptInvitationAsync(request)
            : new AcceptInvitationResult(
                AcceptInvitationResultCode.UnauthorizedError,
                "Unauthorized to accept invitation",
                null
            );

    public async Task<RevokeInvitationResult> RevokeInvitationAsync(
        RevokeInvitationRequest request
    ) =>
        _authorizationProvider.HasAccountContext(request.AccountId)
            ? await _inner.RevokeInvitationAsync(request)
            : new RevokeInvitationResult(
                RevokeInvitationResultCode.UnauthorizedError,
                "Unauthorized to revoke invitation"
            );

    public async Task<RetrieveListResult<Invitation>> ListInvitationsAsync(
        ListInvitationsRequest request
    ) =>
        _authorizationProvider.HasAccountContext(request.AccountId)
            ? await _inner.ListInvitationsAsync(request)
            : new RetrieveListResult<Invitation>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to list invitations",
                null
            );
}
