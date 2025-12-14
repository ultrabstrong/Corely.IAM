namespace Corely.IAM.Models;

public record RegisterPermissionsWithRoleRequest(List<int> PermissionIds, int RoleId);
