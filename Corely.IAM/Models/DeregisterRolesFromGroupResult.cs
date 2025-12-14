namespace Corely.IAM.Models;

public enum DeregisterRolesFromGroupResultCode
{
    Success,
    PartialSuccess,
    InvalidRoleIdsError,
    GroupNotFoundError,
    OwnerRoleRemovalBlockedError,
    UnauthorizedError,
}

public record DeregisterRolesFromGroupResult(
    DeregisterRolesFromGroupResultCode ResultCode,
    string Message,
    int RemovedRoleCount,
    List<int> InvalidRoleIds,
    List<int> BlockedOwnerRoleIds = null!
)
{
    public List<int> BlockedOwnerRoleIds { get; init; } = BlockedOwnerRoleIds ?? [];
}
