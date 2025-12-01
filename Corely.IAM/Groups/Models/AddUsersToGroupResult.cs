namespace Corely.IAM.Groups.Models;

public enum AddUsersToGroupResultCode
{
    Success,
    PartialSuccess,
    InvalidUserIdsError,
    GroupNotFoundError,
}

internal record AddUsersToGroupResult(
    AddUsersToGroupResultCode ResultCode,
    string? Message,
    int AddedUserCount,
    List<int> InvalidUserIds = null
);
