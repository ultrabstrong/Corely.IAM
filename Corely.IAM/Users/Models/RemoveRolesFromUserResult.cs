namespace Corely.IAM.Users.Models;

internal enum RemoveRolesFromUserResultCode
{
    Success,
    PartialSuccess,
    InvalidRoleIdsError,
    UserNotFoundError,
    UserIsSoleOwnerError,
    UnauthorizedError,
}

internal record RemoveRolesFromUserResult(
    RemoveRolesFromUserResultCode ResultCode,
    string Message,
    int RemovedRoleCount,
    List<Guid> InvalidRoleIds,
    List<Guid> BlockedOwnerRoleIds = null!
)
{
    public List<Guid> BlockedOwnerRoleIds { get; init; } = BlockedOwnerRoleIds ?? [];
}
