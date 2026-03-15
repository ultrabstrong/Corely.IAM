using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class InvitationServiceTelemetryDecorator(
    IInvitationService inner,
    ILogger<InvitationServiceTelemetryDecorator> logger
) : IInvitationService
{
    private readonly IInvitationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<InvitationServiceTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<CreateInvitationResult> CreateInvitationAsync(
        CreateInvitationRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(InvitationService),
            request,
            () => _inner.CreateInvitationAsync(request),
            logResult: true
        );

    public async Task<AcceptInvitationResult> AcceptInvitationAsync(
        AcceptInvitationRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(InvitationService),
            request,
            () => _inner.AcceptInvitationAsync(request),
            logResult: true
        );

    public async Task<RevokeInvitationResult> RevokeInvitationAsync(Guid invitationId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(InvitationService),
            invitationId,
            () => _inner.RevokeInvitationAsync(invitationId),
            logResult: true
        );

    public async Task<RetrieveListResult<Invitation>> ListInvitationsAsync(
        ListInvitationsRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(InvitationService),
            request,
            () => _inner.ListInvitationsAsync(request),
            logResult: true
        );
}
