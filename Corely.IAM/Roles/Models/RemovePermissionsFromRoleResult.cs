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
    List<Guid> InvalidPermissionIds,
    List<Guid> SystemPermissionIds = null!
)
{
    public List<Guid> SystemPermissionIds { get; init; } = SystemPermissionIds ?? [];
}
