using Corely.Common.Extensions;
using Corely.IAM.Models;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Services;

internal class DeregistrationServiceAuthorizationDecorator(
    IDeregistrationService inner,
    IAuthorizationProvider authorizationProvider
) : IDeregistrationService
{
    private readonly IDeregistrationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public Task<DeregisterUserResult> DeregisterUserAsync() =>
        _authorizationProvider.HasUserContext()
            ? _inner.DeregisterUserAsync()
            : Task.FromResult(
                new DeregisterUserResult(
                    DeregisterUserResultCode.UnauthorizedError,
                    "Unauthorized to delete user"
                )
            );

    public async Task<DeregisterAccountResult> DeregisterAccountAsync() =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.DeregisterAccountAsync()
            : new DeregisterAccountResult(
                DeregisterAccountResultCode.UnauthorizedError,
                "Unauthorized to delete account"
            );

    public Task<DeregisterGroupResult> DeregisterGroupAsync(DeregisterGroupRequest request) =>
        _authorizationProvider.HasAccountContext()
            ? _inner.DeregisterGroupAsync(request)
            : Task.FromResult(
                new DeregisterGroupResult(
                    DeregisterGroupResultCode.UnauthorizedError,
                    "Unauthorized to delete group"
                )
            );

    public Task<DeregisterRoleResult> DeregisterRoleAsync(DeregisterRoleRequest request) =>
        _authorizationProvider.HasAccountContext()
            ? _inner.DeregisterRoleAsync(request)
            : Task.FromResult(
                new DeregisterRoleResult(
                    DeregisterRoleResultCode.UnauthorizedError,
                    "Unauthorized to delete role"
                )
            );

    public Task<DeregisterPermissionResult> DeregisterPermissionAsync(
        DeregisterPermissionRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? _inner.DeregisterPermissionAsync(request)
            : Task.FromResult(
                new DeregisterPermissionResult(
                    DeregisterPermissionResultCode.UnauthorizedError,
                    "Unauthorized to delete permission"
                )
            );

    public async Task<DeregisterUserFromAccountResult> DeregisterUserFromAccountAsync(
        DeregisterUserFromAccountRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.DeregisterUserFromAccountAsync(request)
            : new DeregisterUserFromAccountResult(
                DeregisterUserFromAccountResultCode.UnauthorizedError,
                "Unauthorized to remove user from account"
            );

    public Task<DeregisterUsersFromGroupResult> DeregisterUsersFromGroupAsync(
        DeregisterUsersFromGroupRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? _inner.DeregisterUsersFromGroupAsync(request)
            : Task.FromResult(
                new DeregisterUsersFromGroupResult(
                    DeregisterUsersFromGroupResultCode.UnauthorizedError,
                    "Unauthorized to remove users from group",
                    0,
                    []
                )
            );

    public Task<DeregisterRolesFromGroupResult> DeregisterRolesFromGroupAsync(
        DeregisterRolesFromGroupRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? _inner.DeregisterRolesFromGroupAsync(request)
            : Task.FromResult(
                new DeregisterRolesFromGroupResult(
                    DeregisterRolesFromGroupResultCode.UnauthorizedError,
                    "Unauthorized to remove roles from group",
                    0,
                    []
                )
            );

    public Task<DeregisterRolesFromUserResult> DeregisterRolesFromUserAsync(
        DeregisterRolesFromUserRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? _inner.DeregisterRolesFromUserAsync(request)
            : Task.FromResult(
                new DeregisterRolesFromUserResult(
                    DeregisterRolesFromUserResultCode.UnauthorizedError,
                    "Unauthorized to remove roles from user",
                    0,
                    []
                )
            );

    public Task<DeregisterPermissionsFromRoleResult> DeregisterPermissionsFromRoleAsync(
        DeregisterPermissionsFromRoleRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? _inner.DeregisterPermissionsFromRoleAsync(request)
            : Task.FromResult(
                new DeregisterPermissionsFromRoleResult(
                    DeregisterPermissionsFromRoleResultCode.UnauthorizedError,
                    "Unauthorized to remove permissions from role",
                    0,
                    []
                )
            );
}
