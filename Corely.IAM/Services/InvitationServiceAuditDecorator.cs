using Corely.Common.Extensions;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.Services;

internal class InvitationServiceAuditDecorator(
    IInvitationService inner,
    IAuditProvider auditProvider
) : IInvitationService
{
    private const string SERVICE = nameof(IInvitationService);

    private readonly IInvitationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<CreateInvitationResult> CreateInvitationAsync(CreateInvitationRequest request) =>
        _auditProvider.RecordAsync(
            OnAccount(AuthAction.Update, request.AccountId),
            () => _inner.CreateInvitationAsync(request),
            r => AuditOutcome.Of(r.ResultCode, r.InvitationId)
        );

    public Task<AcceptInvitationResult> AcceptInvitationAsync(AcceptInvitationRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.ACCOUNT_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => _inner.AcceptInvitationAsync(request),
            r => AuditOutcome.Of(r.ResultCode, r.AccountId) with { AccountId = r.AccountId }
        );

    public Task<RevokeInvitationResult> RevokeInvitationAsync(RevokeInvitationRequest request) =>
        _auditProvider.RecordAsync(
            OnAccount(AuthAction.Update, request.AccountId) with
            {
                ResourceIds = [request.AccountId, request.InvitationId],
            },
            () => _inner.RevokeInvitationAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RetrieveListResult<Invitation>> ListInvitationsAsync(
        ListInvitationsRequest request
    ) =>
        _auditProvider.RecordAsync(
            OnAccount(AuthAction.Read, request.AccountId),
            () => _inner.ListInvitationsAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    private static AuditCall OnAccount(AuthAction action, Guid accountId) =>
        new(SERVICE, action, PermissionConstants.ACCOUNT_RESOURCE_TYPE)
        {
            AccountId = accountId,
            ResourceIds = [accountId],
        };
}
