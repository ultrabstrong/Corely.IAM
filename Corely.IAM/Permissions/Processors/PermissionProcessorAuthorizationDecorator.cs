using Corely.Common.Extensions;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Permissions.Processors;

internal class PermissionProcessorAuthorizationDecorator(
    IPermissionProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IPermissionProcessor
{
    private readonly IPermissionProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<CreatePermissionResult> CreatePermissionAsync(
        CreatePermissionRequest request
    ) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.PERMISSION_RESOURCE_TYPE
        )
            ? await _inner.CreatePermissionAsync(request)
            : new CreatePermissionResult(
                CreatePermissionResultCode.UnauthorizedError,
                "Unauthorized to create permission",
                Guid.Empty
            );

    public Task CreateDefaultSystemPermissionsAsync(Guid accountId) =>
        _inner.CreateDefaultSystemPermissionsAsync(accountId);

    public async Task<DeletePermissionResult> DeletePermissionAsync(Guid permissionId) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Delete,
            PermissionConstants.PERMISSION_RESOURCE_TYPE,
            permissionId
        )
            ? await _inner.DeletePermissionAsync(permissionId)
            : new DeletePermissionResult(
                DeletePermissionResultCode.UnauthorizedError,
                $"Unauthorized to delete permission {permissionId}"
            );
}
