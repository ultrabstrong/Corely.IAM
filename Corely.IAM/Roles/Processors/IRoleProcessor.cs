using Corely.IAM.Roles.Models;

namespace Corely.IAM.Roles.Processors;

internal interface IRoleProcessor
{
    Task<CreateRoleResult> CreateRoleAsync(CreateRoleRequest createRoleRequest);
    Task<CreateDefaultSystemRolesResult> CreateDefaultSystemRolesAsync(int ownerAccountId);
    Task<Role?> GetRoleAsync(int roleId);
    Task<Role?> GetRoleAsync(string roleName, int ownerAccountId);
    Task<AssignPermissionsToRoleResult> AssignPermissionsToRoleAsync(
        AssignPermissionsToRoleRequest request
    );
}
