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
    List<Guid> InvalidRoleIds,
    List<Guid> BlockedOwnerRoleIds = null!
)
{
    public List<Guid> BlockedOwnerRoleIds { get; init; } = BlockedOwnerRoleIds ?? [];
}
