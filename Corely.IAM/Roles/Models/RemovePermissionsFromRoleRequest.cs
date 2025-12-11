namespace Corely.IAM.Roles.Models;

internal record RemovePermissionsFromRoleRequest(
    List<int> PermissionIds,
    int RoleId,
    bool BypassAuthorization = false
);
