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
        await _authorizationProvider.HasAccountContextAsync()
            ? await _inner.DeregisterAccountAsync()
            : new DeregisterAccountResult(
                DeregisterAccountResultCode.UnauthorizedError,
                "Unauthorized to delete account"
            );

    public Task<DeregisterGroupResult> DeregisterGroupAsync(DeregisterGroupRequest request) =>
        _inner.DeregisterGroupAsync(request);

    public Task<DeregisterRoleResult> DeregisterRoleAsync(DeregisterRoleRequest request) =>
        _inner.DeregisterRoleAsync(request);

    public Task<DeregisterPermissionResult> DeregisterPermissionAsync(
        DeregisterPermissionRequest request
    ) => _inner.DeregisterPermissionAsync(request);

    public async Task<DeregisterUserFromAccountResult> DeregisterUserFromAccountAsync(
        DeregisterUserFromAccountRequest request
    ) =>
        await _authorizationProvider.HasAccountContextAsync()
            ? await _inner.DeregisterUserFromAccountAsync(request)
            : new DeregisterUserFromAccountResult(
                DeregisterUserFromAccountResultCode.UnauthorizedError,
                "Unauthorized to remove user from account"
            );

    public Task<DeregisterUsersFromGroupResult> DeregisterUsersFromGroupAsync(
        DeregisterUsersFromGroupRequest request
    ) => _inner.DeregisterUsersFromGroupAsync(request);

    public Task<DeregisterRolesFromGroupResult> DeregisterRolesFromGroupAsync(
        DeregisterRolesFromGroupRequest request
    ) => _inner.DeregisterRolesFromGroupAsync(request);

    public Task<DeregisterRolesFromUserResult> DeregisterRolesFromUserAsync(
        DeregisterRolesFromUserRequest request
    ) => _inner.DeregisterRolesFromUserAsync(request);

    public Task<DeregisterPermissionsFromRoleResult> DeregisterPermissionsFromRoleAsync(
        DeregisterPermissionsFromRoleRequest request
    ) => _inner.DeregisterPermissionsFromRoleAsync(request);
}
