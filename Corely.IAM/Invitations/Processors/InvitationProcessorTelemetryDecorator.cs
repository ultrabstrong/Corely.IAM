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

    public async Task<RevokeInvitationResult> RevokeInvitationAsync(
        RevokeInvitationRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(InvitationProcessor),
            request,
            () => _inner.RevokeInvitationAsync(request),
            logResult: true
        );

    public async Task<ListResult<Invitation>> ListInvitationsAsync(
        ListInvitationsRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(InvitationProcessor),
            request,
            () => _inner.ListInvitationsAsync(request),
            logResult: true
        );
}
