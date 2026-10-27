namespace Corely.IAM.Models;

public record DeregisterApiKeysFromGroupResult(
    DeregisterApiKeysFromGroupResultCode ResultCode,
    string Message,
    int RemovedApiKeyCount,
    List<Guid> InvalidApiKeyIds
);

public enum DeregisterApiKeysFromGroupResultCode
{
    Success,
    PartialSuccess,
    InvalidApiKeyIdsError,
    GroupNotFoundError,
    UnauthorizedError,
}
