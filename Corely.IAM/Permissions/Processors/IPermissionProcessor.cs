using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Security.Models;

namespace Corely.IAM.Permissions.Processors;

internal interface IPermissionProcessor
{
    Task<CreatePermissionResult> CreatePermissionAsync(CreatePermissionRequest request);
    Task CreateDefaultSystemPermissionsAsync(Guid accountId);
    Task<int> CreateMissingOwnerDefaultsAsync();
    Task<ListResult<Permission>> ListPermissionsAsync(
        ListPermissionsRequest request,
        IReadOnlySet<Guid>? authorizedResourceIds = null
    );
    Task<GetResult<Permission>> GetPermissionByIdAsync(
        Guid permissionId,
        bool hydrate,
        AccountScope accountId
    );
    Task<ModifyResult> UpdatePermissionAsync(UpdatePermissionRequest request);
    Task<DeletePermissionResult> DeletePermissionAsync(Guid permissionId, AccountScope accountId);
    Task<List<EffectivePermission>> GetEffectivePermissionsForUserAsync(
        string resourceType,
        Guid resourceId,
        Guid userId,
        Guid accountId
    );
}
