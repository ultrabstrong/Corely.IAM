namespace Corely.IAM.Groups.Models;

public record RemoveUsersFromGroupResult(
    RemoveUsersFromGroupResultCode ResultCode,
    string? Message,
    int RemovedUserCount,
    List<int> InvalidUserIds,
    List<int> SoleOwnerUserIds = null!
)
{
    public List<int> SoleOwnerUserIds { get; init; } = SoleOwnerUserIds ?? [];
}

public enum RemoveUsersFromGroupResultCode
{
    Success,
    PartialSuccess,
    GroupNotFoundError,
    UserIsSoleOwnerError,
    UnauthorizedError,
}
