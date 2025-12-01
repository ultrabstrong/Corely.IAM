namespace Corely.IAM.Roles.Models;

public enum AssignPermissionsToRoleResultCode
{
    Success,
    PartialSuccess,
    InvalidPermissionIdsError,
    RoleNotFoundError,
}

internal record AssignPermissionsToRoleResult(
    AssignPermissionsToRoleResultCode ResultCode,
    string? Message,
    int AddedPermissionCount,
    List<int> InvalidPermissionIds = null
);
