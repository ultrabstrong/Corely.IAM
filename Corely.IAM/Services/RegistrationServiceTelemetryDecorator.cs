using Corely.Common.Extensions;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Extensions;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Corely.IAM.TotpAuths.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class RegistrationServiceTelemetryDecorator(
    IRegistrationService inner,
    ILogger<RegistrationServiceTelemetryDecorator> logger
) : IRegistrationService
{
    private readonly IRegistrationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<RegistrationServiceTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<RegisterUserResult> RegisterUserAsync(RegisterUserRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterUserAsync(request),
            logResult: true
        );

    public async Task<RegisterAccountResult> RegisterAccountAsync(RegisterAccountRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterAccountAsync(request),
            logResult: true
        );

    public async Task<RegisterGroupResult> RegisterGroupAsync(RegisterGroupRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterGroupAsync(request),
            logResult: true
        );

    public async Task<RegisterRoleResult> RegisterRoleAsync(RegisterRoleRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterRoleAsync(request),
            logResult: true
        );

    public async Task<RegisterPermissionResult> RegisterPermissionAsync(
        RegisterPermissionRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterPermissionAsync(request),
            logResult: true
        );

    public async Task<RegisterUserWithAccountResult> RegisterUserWithAccountAsync(
        RegisterUserWithAccountRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterUserWithAccountAsync(request),
            logResult: true
        );

    public async Task<RegisterUsersWithGroupResult> RegisterUsersWithGroupAsync(
        RegisterUsersWithGroupRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterUsersWithGroupAsync(request),
            logResult: true
        );

    public async Task<RegisterRolesWithGroupResult> RegisterRolesWithGroupAsync(
        RegisterRolesWithGroupRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterRolesWithGroupAsync(request),
            logResult: true
        );

    public async Task<RegisterRolesWithUserResult> RegisterRolesWithUserAsync(
        RegisterRolesWithUserRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterRolesWithUserAsync(request),
            logResult: true
        );

    public async Task<RegisterPermissionsWithRoleResult> RegisterPermissionsWithRoleAsync(
        RegisterPermissionsWithRoleRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterPermissionsWithRoleAsync(request),
            logResult: true
        );

    public async Task<EnableTotpResult> EnableTotpAsync() =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            _inner.EnableTotpAsync,
            logResult: true
        );

    public async Task<ConfirmTotpResult> ConfirmTotpAsync(ConfirmTotpRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.ConfirmTotpAsync(request),
            logResult: true
        );

    public async Task<DisableTotpResult> DisableTotpAsync(DisableTotpRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.DisableTotpAsync(request),
            logResult: true
        );

    public async Task<RegenerateTotpRecoveryCodesResult> RegenerateTotpRecoveryCodesAsync() =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            _inner.RegenerateTotpRecoveryCodesAsync,
            logResult: true
        );

    public async Task<LinkGoogleAuthResult> LinkGoogleAuthAsync(LinkGoogleAuthRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.LinkGoogleAuthAsync(request),
            logResult: true
        );

    public async Task<CreateInvitationResult> CreateInvitationAsync(
        CreateInvitationRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.CreateInvitationAsync(request),
            logResult: true
        );

    public async Task<AcceptInvitationResult> AcceptInvitationAsync(
        AcceptInvitationRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.AcceptInvitationAsync(request),
            logResult: true
        );

    public async Task<RevokeInvitationResult> RevokeInvitationAsync(Guid invitationId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            invitationId,
            () => _inner.RevokeInvitationAsync(invitationId),
            logResult: true
        );

    public async Task<RetrieveListResult<Invitation>> ListInvitationsAsync(
        ListInvitationsRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RegistrationService),
            request,
            () => _inner.ListInvitationsAsync(request),
            logResult: true
        );
}
