namespace Corely.IAM.ApiKeys.Models;

public enum CreateApiKeyResultCode
{
    Success,
    ApiKeyExistsError,
    AccountNotFoundError,
    ApiKeysDisabledError,
    UnauthorizedError,
    ValidationError,
}

internal record CreateApiKeyResult(
    CreateApiKeyResultCode ResultCode,
    string Message,
    Guid CreatedId,
    string? Key
);
