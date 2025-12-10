namespace Corely.IAM.Models;

public record DeregisterUsersFromGroupResult(
    DeregisterUsersFromGroupResultCode ResultCode,
    string Message,
    int RemovedUserCount,
    List<int> InvalidUserIds
);

public enum DeregisterUsersFromGroupResultCode
{
    Success,
    PartialSuccess,
    GroupNotFoundError,
}
