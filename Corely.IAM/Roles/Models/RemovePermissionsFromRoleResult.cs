namespace Corely.IAM.Roles.Models;

internal enum RemovePermissionsFromRoleResultCode
{
    Success,
    PartialSuccess,
    InvalidPermissionIdsError,
    RoleNotFoundError,
    SystemPermissionRemovalError,
    UnauthorizedError,
}

internal record RemovePermissionsFromRoleResult(
    RemovePermissionsFromRoleResultCode ResultCode,
    string Message,
    int RemovedPermissionCount,
    List<Guid> InvalidPermissionIds,
    List<Guid> SystemPermissionIds = null!
)
{
    public List<Guid> SystemPermissionIds { get; init; } = SystemPermissionIds ?? [];
}
