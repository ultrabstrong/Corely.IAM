namespace Corely.IAM.Models;

public record DeregisterPermissionsFromRoleRequest(List<Guid> PermissionIds, Guid RoleId);
