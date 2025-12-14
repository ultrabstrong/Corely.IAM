namespace Corely.IAM.Users.Models;

public enum RemoveRolesFromUserResultCode
{
    Success,
    PartialSuccess,
    InvalidRoleIdsError,
    UserNotFoundError,
    UserIsSoleOwnerError,
    UnauthorizedError,
}

public record RemoveRolesFromUserResult(
    RemoveRolesFromUserResultCode ResultCode,
    string? Message,
    int RemovedRoleCount,
    List<int> InvalidRoleIds,
    List<int> BlockedOwnerRoleIds = null!
)
{
    public List<int> BlockedOwnerRoleIds { get; init; } = BlockedOwnerRoleIds ?? [];
}
