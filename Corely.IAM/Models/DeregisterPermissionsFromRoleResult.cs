namespace Corely.IAM.Models;

public record DeregisterPermissionsFromRoleResult(
    DeregisterPermissionsFromRoleResultCode ResultCode,
    string Message,
    int RemovedPermissionCount,
    List<Guid> InvalidPermissionIds,
    List<Guid> SystemPermissionIds = null!
)
{
    public List<Guid> SystemPermissionIds { get; init; } = SystemPermissionIds ?? [];
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
