using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;

namespace Corely.IAM.Permissions.Processors;

internal interface IPermissionProcessor
{
    Task<CreatePermissionResult> CreatePermissionAsync(CreatePermissionRequest request);
    Task CreateDefaultSystemPermissionsAsync(Guid accountId);
    Task<ListResult<Permission>> ListPermissionsAsync(
        FilterBuilder<Permission>? filter,
        OrderBuilder<Permission>? order,
        int skip,
        int take
    );
    Task<GetResult<Permission>> GetPermissionByIdAsync(Guid permissionId, bool hydrate);
    Task<DeletePermissionResult> DeletePermissionAsync(Guid permissionId);
}
