namespace Corely.IAM.Models;

public record DeregisterRolesFromUserResult(
    DeregisterRolesFromUserResultCode ResultCode,
    string Message,
    int RemovedRoleCount,
    List<Guid> InvalidRoleIds,
    List<Guid> BlockedOwnerRoleIds = null!
)
{
    public List<Guid> BlockedOwnerRoleIds { get; init; } = BlockedOwnerRoleIds ?? [];
}

public enum DeregisterRolesFromUserResultCode
{
    Success,
    PartialSuccess,
    InvalidRoleIdsError,
    UserNotFoundError,
    UserIsSoleOwnerError,
    UnauthorizedError,
}
