namespace Corely.IAM.Groups.Models;

public record RemoveUsersFromGroupResult(
    RemoveUsersFromGroupResultCode ResultCode,
    string? Message,
    int RemovedUserCount,
    List<int> InvalidUserIds
);

public enum RemoveUsersFromGroupResultCode
{
    Success,
    PartialSuccess,
    GroupNotFoundError,
}
