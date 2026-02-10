using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Ordering;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;

namespace Corely.IAM.Roles.Processors;

internal interface IRoleProcessor
{
    Task<CreateRoleResult> CreateRoleAsync(CreateRoleRequest createRoleRequest);
    Task<CreateDefaultSystemRolesResult> CreateDefaultSystemRolesAsync(Guid ownerAccountId);
    Task<GetRoleResult> GetRoleAsync(Guid roleId);
    Task<GetRoleResult> GetRoleAsync(string roleName, Guid ownerAccountId);
    Task<ListResult<Role>> ListRolesAsync(
        FilterBuilder<Role>? filter,
        OrderBuilder<Role>? order,
        int skip,
        int take
    );
    Task<GetResult<Role>> GetRoleByIdAsync(Guid roleId, bool hydrate);
    Task<AssignPermissionsToRoleResult> AssignPermissionsToRoleAsync(
        AssignPermissionsToRoleRequest request
    );
    Task<RemovePermissionsFromRoleResult> RemovePermissionsFromRoleAsync(
        RemovePermissionsFromRoleRequest request
    );
    Task<DeleteRoleResult> DeleteRoleAsync(Guid roleId);
}
