using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;

namespace Corely.IAM.Permissions.Processors;

internal interface IPermissionProcessor
{
    Task<CreatePermissionResult> CreatePermissionAsync(CreatePermissionRequest request);
    Task CreateDefaultSystemPermissionsAsync(Guid accountId);
    Task<ListResult<Permission>> ListPermissionsAsync(
        ListPermissionsRequest request,
        IReadOnlySet<Guid>? authorizedResourceIds = null
    );
    Task<GetResult<Permission>> GetPermissionByIdAsync(
        Guid permissionId,
        bool hydrate,
        Guid accountId = default
    );
    Task<DeletePermissionResult> DeletePermissionAsync(Guid permissionId, Guid accountId = default);
    Task<List<EffectivePermission>> GetEffectivePermissionsForUserAsync(
        string resourceType,
        Guid resourceId,
        Guid userId,
        Guid accountId
    );
}
