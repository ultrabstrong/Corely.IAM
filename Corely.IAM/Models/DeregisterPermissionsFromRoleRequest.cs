namespace Corely.IAM.Models;

public record DeregisterPermissionsFromRoleRequest(List<int> PermissionIds, int RoleId);
