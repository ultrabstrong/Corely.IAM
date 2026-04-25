using Corely.Common.Extensions;
using Corely.IAM.Models;
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
        _authorizationProvider.HasAccountContext(request.OwnerAccountId)
        && await _authorizationProvider.IsAuthorizedAsync(
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

    public async Task<ListResult<Permission>> ListPermissionsAsync(
        ListPermissionsRequest request
    ) =>
        _authorizationProvider.HasAccountContext(request.AccountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.PERMISSION_RESOURCE_TYPE
        )
            ? await _inner.ListPermissionsAsync(request)
            : new ListResult<Permission>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to list permissions",
                null
            );

    public async Task<GetResult<Permission>> GetPermissionByIdAsync(
        Guid permissionId,
        bool hydrate,
        Guid accountId = default
    ) =>
        _authorizationProvider.HasAccountContext(accountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.PERMISSION_RESOURCE_TYPE,
            permissionId
        )
            ? await _inner.GetPermissionByIdAsync(permissionId, hydrate, accountId)
            : new GetResult<Permission>(
                RetrieveResultCode.UnauthorizedError,
                $"Unauthorized to read permission {permissionId}",
                null
            );

    public Task<List<EffectivePermission>> GetEffectivePermissionsForUserAsync(
        string resourceType,
        Guid resourceId,
        Guid userId,
        Guid accountId
    ) => _inner.GetEffectivePermissionsForUserAsync(resourceType, resourceId, userId, accountId);

    public async Task<DeletePermissionResult> DeletePermissionAsync(
        Guid permissionId,
        Guid accountId = default
    ) =>
        _authorizationProvider.HasAccountContext(accountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Delete,
            PermissionConstants.PERMISSION_RESOURCE_TYPE,
            permissionId
        )
            ? await _inner.DeletePermissionAsync(permissionId, accountId)
            : new DeletePermissionResult(
                DeletePermissionResultCode.UnauthorizedError,
                $"Unauthorized to delete permission {permissionId}"
            );
}
