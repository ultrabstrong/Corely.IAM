using Corely.IAM.Security.Models;

namespace Corely.IAM.Roles.Models;

internal record RemovePermissionsFromRoleRequest(
    List<Guid> PermissionIds,
    Guid RoleId,
    AccountScope AccountId
);
