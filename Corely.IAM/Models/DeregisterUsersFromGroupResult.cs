namespace Corely.IAM.Models;

public record DeregisterUsersFromGroupResult(
    DeregisterUsersFromGroupResultCode ResultCode,
    string Message,
    int RemovedUserCount,
    List<Guid> InvalidUserIds,
    List<Guid> SoleOwnerUserIds = null!
)
{
    public List<Guid> SoleOwnerUserIds { get; init; } = SoleOwnerUserIds ?? [];
}

public enum DeregisterUsersFromGroupResultCode
{
    Success,
    PartialSuccess,
    GroupNotFoundError,
    UserIsSoleOwnerError,
    UnauthorizedError,
}
