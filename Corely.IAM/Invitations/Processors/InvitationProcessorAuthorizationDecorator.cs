using Corely.Common.Extensions;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Invitations.Processors;

internal class InvitationProcessorAuthorizationDecorator(
    IInvitationProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IInvitationProcessor
{
    private readonly IInvitationProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<CreateInvitationResult> CreateInvitationAsync(
        CreateInvitationRequest request
    ) =>
        _authorizationProvider.HasAccountContext(request.AccountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            request.AccountId
        )
            ? await _inner.CreateInvitationAsync(request)
            : new CreateInvitationResult(
                CreateInvitationResultCode.UnauthorizedError,
                $"Unauthorized to create invitation for account {request.AccountId}",
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
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            request.AccountId
        )
            ? await _inner.RevokeInvitationAsync(request)
            : new RevokeInvitationResult(
                RevokeInvitationResultCode.UnauthorizedError,
                $"Unauthorized to revoke invitation for account {request.AccountId}"
            );

    public async Task<ListResult<Invitation>> ListInvitationsAsync(
        ListInvitationsRequest request
    ) =>
        _authorizationProvider.HasAccountContext(request.AccountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            request.AccountId
        )
            ? await _inner.ListInvitationsAsync(request)
            : new ListResult<Invitation>(
                RetrieveResultCode.UnauthorizedError,
                $"Unauthorized to list invitations for account {request.AccountId}",
                null
            );
}
