namespace Corely.IAM.Roles.Models;

public enum RemovePermissionsFromRoleResultCode
{
    Success,
    PartialSuccess,
    InvalidPermissionIdsError,
    RoleNotFoundError,
    SystemPermissionRemovalError,
    UnauthorizedError,
}

public record RemovePermissionsFromRoleResult(
    RemovePermissionsFromRoleResultCode ResultCode,
    string? Message,
    int RemovedPermissionCount,
    List<int> InvalidPermissionIds,
    List<int> SystemPermissionIds = null!
)
{
    public List<int> SystemPermissionIds { get; init; } = SystemPermissionIds ?? [];
}
