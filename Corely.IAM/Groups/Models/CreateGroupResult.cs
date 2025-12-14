namespace Corely.IAM.Groups.Models;

public enum CreateGroupResultCode
{
    Success,
    GroupExistsError,
    AccountNotFoundError,
    UnauthorizedError,
}

internal record CreateGroupResult(CreateGroupResultCode ResultCode, string Message, int CreatedId);
