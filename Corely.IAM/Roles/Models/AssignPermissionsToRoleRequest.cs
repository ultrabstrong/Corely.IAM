namespace Corely.IAM.Roles.Models;

internal record AssignPermissionsToRoleRequest(List<Guid> PermissionIds, Guid RoleId);
