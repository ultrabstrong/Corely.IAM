namespace Corely.IAM.Models;

public record DeregisterPermissionsFromRoleResult(
    DeregisterPermissionsFromRoleResultCode ResultCode,
    string Message,
    int RemovedPermissionCount,
    List<int> InvalidPermissionIds,
    List<int> SystemPermissionIds = null!
)
{
    public List<int> SystemPermissionIds { get; init; } = SystemPermissionIds ?? [];
}

public enum DeregisterPermissionsFromRoleResultCode
{
    Success,
    PartialSuccess,
    InvalidPermissionIdsError,
    RoleNotFoundError,
    SystemPermissionRemovalError,
    UnauthorizedError,
}
