using Corely.IAM.Groups.Models;

namespace Corely.IAM.Models;

public record RegisterApiKeysWithGroupResult(
    AddApiKeysToGroupResultCode ResultCode,
    string Message,
    int RegisteredApiKeyCount,
    List<Guid> InvalidApiKeyIds
);
