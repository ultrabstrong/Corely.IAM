namespace Corely.IAM.Groups.Models;

public enum AddApiKeysToGroupResultCode
{
    Success,
    PartialSuccess,
    InvalidApiKeyIdsError,
    GroupNotFoundError,
    UnauthorizedError,
}

internal record AddApiKeysToGroupResult(
    AddApiKeysToGroupResultCode ResultCode,
    string Message,
    int AddedApiKeyCount,
    List<Guid> InvalidApiKeyIds
);
