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
    List<Guid> InvalidRoleIds,
    List<Guid> BlockedOwnerRoleIds = null!
)
{
    public List<Guid> BlockedOwnerRoleIds { get; init; } = BlockedOwnerRoleIds ?? [];
}
