namespace Corely.IAM.Groups.Models;

public enum RemoveRolesFromGroupResultCode
{
    Success,
    PartialSuccess,
    InvalidRoleIdsError,
    GroupNotFoundError,
    OwnerRoleRemovalBlockedError,
    UnauthorizedError,
}

public record RemoveRolesFromGroupResult(
    RemoveRolesFromGroupResultCode ResultCode,
    string? Message,
    int RemovedRoleCount,
    List<Guid> InvalidRoleIds,
    List<Guid> BlockedOwnerRoleIds = null!
)
{
    public List<Guid> BlockedOwnerRoleIds { get; init; } = BlockedOwnerRoleIds ?? [];
}
