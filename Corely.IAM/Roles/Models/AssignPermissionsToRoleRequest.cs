namespace Corely.IAM.Roles.Models;

internal record AssignPermissionsToRoleRequest(
    List<int> PermissionIds,
    int RoleId,
    bool BypassAuthorization = false
);
