using Corely.Common.Extensions;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Processors;

namespace Corely.IAM.Permissions.Processors;

internal class PermissionProcessorAuthorizationDecorator(
    IPermissionProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IPermissionProcessor
{
    private readonly IPermissionProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<CreatePermissionResult> CreatePermissionAsync(CreatePermissionRequest request)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.PERMISSION_RESOURCE_TYPE,
            AuthAction.Create
        );
        return await _inner.CreatePermissionAsync(request);
    }

    public Task CreateDefaultSystemPermissionsAsync(int accountId) =>
        _inner.CreateDefaultSystemPermissionsAsync(accountId);

    public async Task<DeletePermissionResult> DeletePermissionAsync(int permissionId)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.PERMISSION_RESOURCE_TYPE,
            AuthAction.Delete,
            permissionId
        );
        return await _inner.DeletePermissionAsync(permissionId);
    }
}
