namespace Corely.IAM.Groups.Models;

public record RemoveUsersFromGroupResult(
    RemoveUsersFromGroupResultCode ResultCode,
    string? Message,
    int RemovedUserCount,
    List<Guid> InvalidUserIds,
    List<Guid> SoleOwnerUserIds = null!
)
{
    public List<Guid> SoleOwnerUserIds { get; init; } = SoleOwnerUserIds ?? [];
}

public enum RemoveUsersFromGroupResultCode
{
    Success,
    PartialSuccess,
    GroupNotFoundError,
    UserIsSoleOwnerError,
    UnauthorizedError,
}
