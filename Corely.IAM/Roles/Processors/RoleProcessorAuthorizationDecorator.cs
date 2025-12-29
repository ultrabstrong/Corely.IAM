using Corely.Common.Extensions;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Roles.Processors;

internal class RoleProcessorAuthorizationDecorator(
    IRoleProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IRoleProcessor
{
    private readonly IRoleProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<CreateRoleResult> CreateRoleAsync(CreateRoleRequest createRoleRequest) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.ROLE_RESOURCE_TYPE
        )
            ? await _inner.CreateRoleAsync(createRoleRequest)
            : new CreateRoleResult(
                CreateRoleResultCode.UnauthorizedError,
                "Unauthorized to create role",
                Guid.Empty
            );

    public Task<CreateDefaultSystemRolesResult> CreateDefaultSystemRolesAsync(
        Guid ownerAccountId
    ) => _inner.CreateDefaultSystemRolesAsync(ownerAccountId);

    public async Task<GetRoleResult> GetRoleAsync(Guid roleId) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.ROLE_RESOURCE_TYPE,
            roleId
        )
            ? await _inner.GetRoleAsync(roleId)
            : new GetRoleResult(
                GetRoleResultCode.UnauthorizedError,
                $"Unauthorized to read role {roleId}",
                null
            );

    public async Task<GetRoleResult> GetRoleAsync(string roleName, Guid ownerAccountId) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.ROLE_RESOURCE_TYPE
        )
            ? await _inner.GetRoleAsync(roleName, ownerAccountId)
            : new GetRoleResult(
                GetRoleResultCode.UnauthorizedError,
                $"Unauthorized to read role {roleName}",
                null
            );

    public async Task<AssignPermissionsToRoleResult> AssignPermissionsToRoleAsync(
        AssignPermissionsToRoleRequest request
    ) =>
        request.BypassAuthorization
        || (
            await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Update,
                PermissionConstants.ROLE_RESOURCE_TYPE,
                request.RoleId
            )
            && await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Read,
                PermissionConstants.PERMISSION_RESOURCE_TYPE,
                [.. request.PermissionIds]
            )
        )
            ? await _inner.AssignPermissionsToRoleAsync(request)
            : new AssignPermissionsToRoleResult(
                AssignPermissionsToRoleResultCode.UnauthorizedError,
                $"Unauthorized to update role {request.RoleId} or read permissions",
                0,
                []
            );

    public async Task<RemovePermissionsFromRoleResult> RemovePermissionsFromRoleAsync(
        RemovePermissionsFromRoleRequest request
    ) =>
        request.BypassAuthorization
        || (
            await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Update,
                PermissionConstants.ROLE_RESOURCE_TYPE,
                request.RoleId
            )
            && await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Read,
                PermissionConstants.PERMISSION_RESOURCE_TYPE,
                [.. request.PermissionIds]
            )
        )
            ? await _inner.RemovePermissionsFromRoleAsync(request)
            : new RemovePermissionsFromRoleResult(
                RemovePermissionsFromRoleResultCode.UnauthorizedError,
                $"Unauthorized to update role {request.RoleId} or read permissions",
                0,
                []
            );

    public async Task<DeleteRoleResult> DeleteRoleAsync(Guid roleId) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Delete,
            PermissionConstants.ROLE_RESOURCE_TYPE,
            roleId
        )
            ? await _inner.DeleteRoleAsync(roleId)
            : new DeleteRoleResult(
                DeleteRoleResultCode.UnauthorizedError,
                $"Unauthorized to delete role {roleId}"
            );
}
