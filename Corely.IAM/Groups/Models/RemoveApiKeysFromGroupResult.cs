namespace Corely.IAM.Groups.Models;

internal record RemoveApiKeysFromGroupResult(
    RemoveApiKeysFromGroupResultCode ResultCode,
    string Message,
    int RemovedApiKeyCount,
    List<Guid> InvalidApiKeyIds
);

internal enum RemoveApiKeysFromGroupResultCode
{
    Success,
    PartialSuccess,
    InvalidApiKeyIdsError,
    GroupNotFoundError,
    UnauthorizedError,
}
