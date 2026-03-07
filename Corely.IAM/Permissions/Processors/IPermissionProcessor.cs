using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;

namespace Corely.IAM.Permissions.Processors;

internal interface IPermissionProcessor
{
    Task<CreatePermissionResult> CreatePermissionAsync(CreatePermissionRequest request);
    Task CreateDefaultSystemPermissionsAsync(Guid accountId);
    Task<ListResult<Permission>> ListPermissionsAsync(ListPermissionsRequest request);
    Task<GetResult<Permission>> GetPermissionByIdAsync(Guid permissionId, bool hydrate);
    Task<DeletePermissionResult> DeletePermissionAsync(Guid permissionId);
    Task<List<EffectivePermission>> GetEffectivePermissionsForUserAsync(
        string resourceType,
        Guid resourceId,
        Guid userId,
        Guid accountId
    );
}
