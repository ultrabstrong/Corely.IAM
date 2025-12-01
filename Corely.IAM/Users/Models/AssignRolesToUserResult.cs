namespace Corely.IAM.Users.Models;

public enum AssignRolesToUserResultCode
{
    Success,
    PartialSuccess,
    InvalidRoleIdsError,
    UserNotFoundError,
}

internal record AssignRolesToUserResult(
    AssignRolesToUserResultCode ResultCode,
    string? Message,
    int AddedRoleCount,
    List<int> InvalidRoleIds = null
);
