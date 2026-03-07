namespace Corely.IAM.Groups.Models;

public enum CreateGroupResultCode
{
    Success,
    GroupExistsError,
    AccountNotFoundError,
    UnauthorizedError,
    ValidationError,
}

internal record CreateGroupResult(CreateGroupResultCode ResultCode, string Message, Guid CreatedId);
