namespace Corely.IAM.Models;

public record DeregisterRolesFromUserResult(
    DeregisterRolesFromUserResultCode ResultCode,
    string Message,
    int RemovedRoleCount,
    List<int> InvalidRoleIds,
    List<int> BlockedOwnerRoleIds = null!
)
{
    public List<int> BlockedOwnerRoleIds { get; init; } = BlockedOwnerRoleIds ?? [];
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
