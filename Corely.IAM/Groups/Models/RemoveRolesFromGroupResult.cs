namespace Corely.IAM.Groups.Models;

internal enum RemoveRolesFromGroupResultCode
{
    Success,
    PartialSuccess,
    InvalidRoleIdsError,
    GroupNotFoundError,
    OwnerRoleRemovalBlockedError,
    UnauthorizedError,
}

internal record RemoveRolesFromGroupResult(
    RemoveRolesFromGroupResultCode ResultCode,
    string Message,
    int RemovedRoleCount,
    List<Guid> InvalidRoleIds,
    List<Guid> BlockedOwnerRoleIds = null!
)
{
    public List<Guid> BlockedOwnerRoleIds { get; init; } = BlockedOwnerRoleIds ?? [];
}
