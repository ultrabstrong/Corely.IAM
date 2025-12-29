using Corely.IAM.Permissions.Models;

namespace Corely.IAM.Permissions.Processors;

internal interface IPermissionProcessor
{
    Task<CreatePermissionResult> CreatePermissionAsync(CreatePermissionRequest request);
    Task CreateDefaultSystemPermissionsAsync(Guid accountId);
    Task<DeletePermissionResult> DeletePermissionAsync(Guid permissionId);
}
