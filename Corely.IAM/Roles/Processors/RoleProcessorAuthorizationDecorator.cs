using Corely.Common.Extensions;
using Corely.IAM.Auth.Constants;
using Corely.IAM.Auth.Providers;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Roles.Models;

namespace Corely.IAM.Roles.Processors;

internal class RoleProcessorAuthorizationDecorator(
    IRoleProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IRoleProcessor
{
    private readonly IRoleProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<CreateRoleResult> CreateRoleAsync(CreateRoleRequest createRoleRequest)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.ROLE_RESOURCE_TYPE,
            AuthAction.Create
        );
        return await _inner.CreateRoleAsync(createRoleRequest);
    }

    public Task<CreateDefaultSystemRolesResult> CreateDefaultSystemRolesAsync(int ownerAccountId) =>
        _inner.CreateDefaultSystemRolesAsync(ownerAccountId);

    public async Task<Role?> GetRoleAsync(int roleId)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.ROLE_RESOURCE_TYPE,
            AuthAction.Read,
            roleId
        );
        return await _inner.GetRoleAsync(roleId);
    }

    public async Task<Role?> GetRoleAsync(string roleName, int ownerAccountId)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.ROLE_RESOURCE_TYPE,
            AuthAction.Read
        );
        return await _inner.GetRoleAsync(roleName, ownerAccountId);
    }

    public async Task<AssignPermissionsToRoleResult> AssignPermissionsToRoleAsync(
        AssignPermissionsToRoleRequest request
    )
    {
        if (!request.BypassAuthorization)
            await _authorizationProvider.AuthorizeAsync(
                PermissionConstants.ROLE_RESOURCE_TYPE,
                AuthAction.Update,
                request.RoleId
            );
        return await _inner.AssignPermissionsToRoleAsync(request);
    }
}
