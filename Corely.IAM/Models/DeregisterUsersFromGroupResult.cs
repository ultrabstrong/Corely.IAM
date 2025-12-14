namespace Corely.IAM.Models;

public record DeregisterUsersFromGroupResult(
    DeregisterUsersFromGroupResultCode ResultCode,
    string Message,
    int RemovedUserCount,
    List<int> InvalidUserIds,
    List<int> SoleOwnerUserIds = null!
)
{
    public List<int> SoleOwnerUserIds { get; init; } = SoleOwnerUserIds ?? [];
}

public enum DeregisterUsersFromGroupResultCode
{
    Success,
    PartialSuccess,
    GroupNotFoundError,
    UserIsSoleOwnerError,
    UnauthorizedError,
}
