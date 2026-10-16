namespace Corely.IAM.Groups.Models;

internal record RemoveUsersFromGroupResult(
    RemoveUsersFromGroupResultCode ResultCode,
    string Message,
    int RemovedUserCount,
    List<Guid> InvalidUserIds,
    List<Guid> SoleOwnerUserIds = null!
)
{
    public List<Guid> SoleOwnerUserIds { get; init; } = SoleOwnerUserIds ?? [];
}

internal enum RemoveUsersFromGroupResultCode
{
    Success,
    PartialSuccess,
    GroupNotFoundError,
    UserIsSoleOwnerError,
    UnauthorizedError,
}
