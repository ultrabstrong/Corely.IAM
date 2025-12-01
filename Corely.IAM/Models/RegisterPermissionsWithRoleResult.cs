using Corely.IAM.Roles.Models;

namespace Corely.IAM.Models;

public record RegisterPermissionsWithRoleResult(
    AssignPermissionsToRoleResultCode ResultCode,
    string Message,
    int RegisteredPermissionCount,
    List<int> InvalidPermissionIds = null
);
