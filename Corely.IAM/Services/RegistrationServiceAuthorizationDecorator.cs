using Corely.Common.Extensions;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Services;

internal class RegistrationServiceAuthorizationDecorator(
    IRegistrationService inner,
    IAuthorizationProvider authorizationProvider
) : IRegistrationService
{
    private readonly IRegistrationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public Task<RegisterUserResult> RegisterUserAsync(RegisterUserRequest request) =>
        _inner.RegisterUserAsync(request);

    public async Task<RegisterAccountResult> RegisterAccountAsync(RegisterAccountRequest request) =>
        _authorizationProvider.HasUserContext()
            ? await _inner.RegisterAccountAsync(request)
            : new RegisterAccountResult(
                RegisterAccountResultCode.UnauthorizedError,
                "Unauthorized to create account",
                Guid.Empty
            );

    public async Task<RegisterGroupResult> RegisterGroupAsync(RegisterGroupRequest request) =>
        await _authorizationProvider.HasAccountContextAsync()
            ? await _inner.RegisterGroupAsync(request)
            : new RegisterGroupResult(
                CreateGroupResultCode.UnauthorizedError,
                "Unauthorized to create group",
                -1
            );

    public async Task<RegisterRoleResult> RegisterRoleAsync(RegisterRoleRequest request) =>
        await _authorizationProvider.HasAccountContextAsync()
            ? await _inner.RegisterRoleAsync(request)
            : new RegisterRoleResult(
                CreateRoleResultCode.UnauthorizedError,
                "Unauthorized to create role",
                -1
            );

    public async Task<RegisterPermissionResult> RegisterPermissionAsync(
        RegisterPermissionRequest request
    ) =>
        await _authorizationProvider.HasAccountContextAsync()
            ? await _inner.RegisterPermissionAsync(request)
            : new RegisterPermissionResult(
                CreatePermissionResultCode.UnauthorizedError,
                "Unauthorized to create permission",
                -1
            );

    public async Task<RegisterUserWithAccountResult> RegisterUserWithAccountAsync(
        RegisterUserWithAccountRequest request
    ) =>
        await _authorizationProvider.HasAccountContextAsync()
            ? await _inner.RegisterUserWithAccountAsync(request)
            : new RegisterUserWithAccountResult(
                RegisterUserWithAccountResultCode.UnauthorizedError,
                "Unauthorized to add user to account"
            );

    public Task<RegisterUsersWithGroupResult> RegisterUsersWithGroupAsync(
        RegisterUsersWithGroupRequest request
    ) => _inner.RegisterUsersWithGroupAsync(request);

    public Task<RegisterRolesWithGroupResult> RegisterRolesWithGroupAsync(
        RegisterRolesWithGroupRequest request
    ) => _inner.RegisterRolesWithGroupAsync(request);

    public Task<RegisterRolesWithUserResult> RegisterRolesWithUserAsync(
        RegisterRolesWithUserRequest request
    ) => _inner.RegisterRolesWithUserAsync(request);

    public Task<RegisterPermissionsWithRoleResult> RegisterPermissionsWithRoleAsync(
        RegisterPermissionsWithRoleRequest request
    ) => _inner.RegisterPermissionsWithRoleAsync(request);
}
