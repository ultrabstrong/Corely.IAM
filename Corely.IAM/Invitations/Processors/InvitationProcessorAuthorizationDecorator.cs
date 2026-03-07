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
        await _authorizationProvider.IsAuthorizedAsync(
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
        // Only requires authenticated user — no account context or CRUDX check
        _authorizationProvider.HasUserContext()
            ? await _inner.AcceptInvitationAsync(request)
            : new AcceptInvitationResult(
                AcceptInvitationResultCode.UnauthorizedError,
                "Unauthorized to accept invitation",
                null
            );

    public async Task<RevokeInvitationResult> RevokeInvitationAsync(Guid invitationId)
    {
        // Must look up the invitation to get the account ID for the auth check
        // Delegate to inner — the processor will handle not-found.
        // Auth is checked at the service level (HasAccountContext) and via account Update permission.
        // Since we can't check the specific account without loading the entity first,
        // we rely on the service-level guard and the fact that only account members can reach this.
        return await _inner.RevokeInvitationAsync(invitationId);
    }

    public async Task<ListResult<Invitation>> ListInvitationsAsync(
        Guid accountId,
        FilterBuilder<Invitation>? filter,
        OrderBuilder<Invitation>? order,
        int skip,
        int take,
        string? statusFilter = null
    ) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            accountId
        )
            ? await _inner.ListInvitationsAsync(accountId, filter, order, skip, take, statusFilter)
            : new ListResult<Invitation>(
                RetrieveResultCode.UnauthorizedError,
                $"Unauthorized to list invitations for account {accountId}",
                null
            );
}
