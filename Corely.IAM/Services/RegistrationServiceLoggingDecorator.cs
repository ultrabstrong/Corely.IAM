using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class RegistrationServiceLoggingDecorator(
    IRegistrationService inner,
    ILogger<RegistrationServiceLoggingDecorator> logger
) : IRegistrationService
{
    private readonly IRegistrationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<RegistrationServiceLoggingDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<RegisterUserResult> RegisterUserAsync(RegisterUserRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterUserAsync(request),
            logResult: true
        );

    public async Task<RegisterAccountResult> RegisterAccountAsync(RegisterAccountRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterAccountAsync(request),
            logResult: true
        );

    public async Task<RegisterGroupResult> RegisterGroupAsync(RegisterGroupRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterGroupAsync(request),
            logResult: true
        );

    public async Task<RegisterRoleResult> RegisterRoleAsync(RegisterRoleRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterRoleAsync(request),
            logResult: true
        );

    public async Task<RegisterPermissionResult> RegisterPermissionAsync(
        RegisterPermissionRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterPermissionAsync(request),
            logResult: true
        );

    public async Task<RegisterUserWithAccountResult> RegisterUserWithAccountAsync(
        RegisterUserWithAccountRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterUserWithAccountAsync(request),
            logResult: true
        );

    public async Task<RegisterUsersWithGroupResult> RegisterUsersWithGroupAsync(
        RegisterUsersWithGroupRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterUsersWithGroupAsync(request),
            logResult: true
        );

    public async Task<RegisterRolesWithGroupResult> RegisterRolesWithGroupAsync(
        RegisterRolesWithGroupRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterRolesWithGroupAsync(request),
            logResult: true
        );

    public async Task<RegisterRolesWithUserResult> RegisterRolesWithUserAsync(
        RegisterRolesWithUserRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterRolesWithUserAsync(request),
            logResult: true
        );

    public async Task<RegisterPermissionsWithRoleResult> RegisterPermissionsWithRoleAsync(
        RegisterPermissionsWithRoleRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(RegistrationService),
            request,
            () => _inner.RegisterPermissionsWithRoleAsync(request),
            logResult: true
        );
}
