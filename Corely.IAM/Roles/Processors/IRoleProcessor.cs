using Corely.IAM.Roles.Models;

namespace Corely.IAM.Roles.Processors;

internal interface IRoleProcessor
{
    Task<CreateRoleResult> CreateRoleAsync(CreateRoleRequest createRoleRequest);
    Task<CreateDefaultSystemRolesResult> CreateDefaultSystemRolesAsync(Guid ownerAccountId);
    Task<GetRoleResult> GetRoleAsync(Guid roleId);
    Task<GetRoleResult> GetRoleAsync(string roleName, Guid ownerAccountId);
    Task<AssignPermissionsToRoleResult> AssignPermissionsToRoleAsync(
        AssignPermissionsToRoleRequest request
    );
    Task<RemovePermissionsFromRoleResult> RemovePermissionsFromRoleAsync(
        RemovePermissionsFromRoleRequest request
    );
    Task<DeleteRoleResult> DeleteRoleAsync(Guid roleId);
}
