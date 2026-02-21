namespace Corely.IAM.Roles.Models;

internal record RemovePermissionsFromRoleRequest(List<Guid> PermissionIds, Guid RoleId);
