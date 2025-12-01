namespace Corely.IAM.Groups.Models;

public enum AssignRolesToGroupResultCode
{
    Success,
    PartialSuccess,
    InvalidRoleIdsError,
    GroupNotFoundError,
}

internal record AssignRolesToGroupResult(
    AssignRolesToGroupResultCode ResultCode,
    string? Message,
    int AddedRoleCount,
    List<int> InvalidRoleIds = null
);
