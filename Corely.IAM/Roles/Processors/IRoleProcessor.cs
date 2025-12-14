using Corely.IAM.Roles.Models;

namespace Corely.IAM.Roles.Processors;

internal interface IRoleProcessor
{
    Task<CreateRoleResult> CreateRoleAsync(CreateRoleRequest createRoleRequest);
    Task<CreateDefaultSystemRolesResult> CreateDefaultSystemRolesAsync(int ownerAccountId);
    Task<GetRoleResult> GetRoleAsync(int roleId);
    Task<GetRoleResult> GetRoleAsync(string roleName, int ownerAccountId);
    Task<AssignPermissionsToRoleResult> AssignPermissionsToRoleAsync(
        AssignPermissionsToRoleRequest request
    );
    Task<RemovePermissionsFromRoleResult> RemovePermissionsFromRoleAsync(
        RemovePermissionsFromRoleRequest request
    );
    Task<DeleteRoleResult> DeleteRoleAsync(int roleId);
}
