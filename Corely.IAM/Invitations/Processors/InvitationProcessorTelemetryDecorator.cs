using Corely.Common.Extensions;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Extensions;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Invitations.Processors;

internal class InvitationProcessorTelemetryDecorator(
    IInvitationProcessor inner,
    ILogger<InvitationProcessorTelemetryDecorator> logger
) : IInvitationProcessor
{
    private readonly IInvitationProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<InvitationProcessorTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<CreateInvitationResult> CreateInvitationAsync(
        CreateInvitationRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(InvitationProcessor),
            request,
            () => _inner.CreateInvitationAsync(request),
            logResult: true
        );

    public async Task<AcceptInvitationResult> AcceptInvitationAsync(
        AcceptInvitationRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(InvitationProcessor),
            request,
            () => _inner.AcceptInvitationAsync(request),
            logResult: true
        );

    public async Task<RevokeInvitationResult> RevokeInvitationAsync(Guid invitationId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(InvitationProcessor),
            invitationId,
            () => _inner.RevokeInvitationAsync(invitationId),
            logResult: true
        );

    public async Task<ListResult<Invitation>> ListInvitationsAsync(
        Guid accountId,
        FilterBuilder<Invitation>? filter,
        OrderBuilder<Invitation>? order,
        int skip,
        int take,
        InvitationStatus? statusFilter = null
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(InvitationProcessor),
            accountId,
            () => _inner.ListInvitationsAsync(accountId, filter, order, skip, take, statusFilter),
            logResult: true
        );
}
