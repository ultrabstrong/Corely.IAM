using Corely.IAM.ApiKeys.Models;

namespace Corely.IAM.Models;

public record RegisterApiKeyResult(
    CreateApiKeyResultCode ResultCode,
    string Message,
    Guid CreatedApiKeyId,
    string? Key
);
