namespace Corely.IAM.Models;

public record RegisterPermissionsWithRoleRequest(List<Guid> PermissionIds, Guid RoleId);
