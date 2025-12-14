using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class DeregistrationServiceLoggingDecorator(
    IDeregistrationService inner,
    ILogger<DeregistrationServiceLoggingDecorator> logger
) : IDeregistrationService
{
    private readonly IDeregistrationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<DeregistrationServiceLoggingDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<DeregisterUserResult> DeregisterUserAsync() =>
        await _logger.ExecuteWithLogging(
            nameof(DeregistrationService),
            () => _inner.DeregisterUserAsync(),
            logResult: true
        );

    public async Task<DeregisterAccountResult> DeregisterAccountAsync() =>
        await _logger.ExecuteWithLogging(
            nameof(DeregistrationService),
            () => _inner.DeregisterAccountAsync(),
            logResult: true
        );

    public async Task<DeregisterGroupResult> DeregisterGroupAsync(DeregisterGroupRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(DeregistrationService),
            request,
            () => _inner.DeregisterGroupAsync(request),
            logResult: true
        );

    public async Task<DeregisterRoleResult> DeregisterRoleAsync(DeregisterRoleRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(DeregistrationService),
            request,
            () => _inner.DeregisterRoleAsync(request),
            logResult: true
        );

    public async Task<DeregisterPermissionResult> DeregisterPermissionAsync(
        DeregisterPermissionRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(DeregistrationService),
            request,
            () => _inner.DeregisterPermissionAsync(request),
            logResult: true
        );

    public async Task<DeregisterUserFromAccountResult> DeregisterUserFromAccountAsync(
        DeregisterUserFromAccountRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(DeregistrationService),
            request,
            () => _inner.DeregisterUserFromAccountAsync(request),
            logResult: true
        );

    public async Task<DeregisterUsersFromGroupResult> DeregisterUsersFromGroupAsync(
        DeregisterUsersFromGroupRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(DeregistrationService),
            request,
            () => _inner.DeregisterUsersFromGroupAsync(request),
            logResult: true
        );

    public async Task<DeregisterRolesFromGroupResult> DeregisterRolesFromGroupAsync(
        DeregisterRolesFromGroupRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(DeregistrationService),
            request,
            () => _inner.DeregisterRolesFromGroupAsync(request),
            logResult: true
        );

    public async Task<DeregisterRolesFromUserResult> DeregisterRolesFromUserAsync(
        DeregisterRolesFromUserRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(DeregistrationService),
            request,
            () => _inner.DeregisterRolesFromUserAsync(request),
            logResult: true
        );

    public async Task<DeregisterPermissionsFromRoleResult> DeregisterPermissionsFromRoleAsync(
        DeregisterPermissionsFromRoleRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(DeregistrationService),
            request,
            () => _inner.DeregisterPermissionsFromRoleAsync(request),
            logResult: true
        );
}
