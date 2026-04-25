using Corely.Common.Extensions;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;

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

    public Task<RegisterUserWithGoogleResult> RegisterUserWithGoogleAsync(
        RegisterUserWithGoogleRequest request
    ) => _inner.RegisterUserWithGoogleAsync(request);

    public async Task<RegisterAccountResult> RegisterAccountAsync(RegisterAccountRequest request) =>
        await _inner.RegisterAccountAsync(request);

    public async Task<RegisterGroupResult> RegisterGroupAsync(RegisterGroupRequest request) =>
        await _inner.RegisterGroupAsync(request);

    public async Task<RegisterRoleResult> RegisterRoleAsync(RegisterRoleRequest request) =>
        await _inner.RegisterRoleAsync(request);

    public async Task<RegisterPermissionResult> RegisterPermissionAsync(
        RegisterPermissionRequest request
    ) => await _inner.RegisterPermissionAsync(request);

    public async Task<RegisterUserWithAccountResult> RegisterUserWithAccountAsync(
        RegisterUserWithAccountRequest request
    ) => await _inner.RegisterUserWithAccountAsync(request);

    public async Task<RegisterUsersWithGroupResult> RegisterUsersWithGroupAsync(
        RegisterUsersWithGroupRequest request
    ) => await _inner.RegisterUsersWithGroupAsync(request);

    public async Task<RegisterRolesWithGroupResult> RegisterRolesWithGroupAsync(
        RegisterRolesWithGroupRequest request
    ) => await _inner.RegisterRolesWithGroupAsync(request);

    public async Task<RegisterRolesWithUserResult> RegisterRolesWithUserAsync(
        RegisterRolesWithUserRequest request
    ) => await _inner.RegisterRolesWithUserAsync(request);

    public async Task<RegisterPermissionsWithRoleResult> RegisterPermissionsWithRoleAsync(
        RegisterPermissionsWithRoleRequest request
    ) => await _inner.RegisterPermissionsWithRoleAsync(request);

    public async Task<SetPasswordResult> SetPasswordAsync(SetPasswordRequest request) =>
        _authorizationProvider.IsNonSystemUserContext()
            ? await _inner.SetPasswordAsync(request)
            : new SetPasswordResult(SetPasswordResultCode.UnauthorizedError, "Unauthorized");
}
