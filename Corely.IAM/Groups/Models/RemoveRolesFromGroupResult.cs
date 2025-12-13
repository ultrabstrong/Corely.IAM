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
    List<int> InvalidRoleIds,
    List<int> BlockedOwnerRoleIds = null!
)
{
    public List<int> BlockedOwnerRoleIds { get; init; } = BlockedOwnerRoleIds ?? [];
}
