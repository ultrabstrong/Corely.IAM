namespace Corely.IAM.ApiKeys.Models;

public enum RotateApiKeySecretResultCode
{
    Success,
    ApiKeyNotFoundError,
    ApiKeysDisabledError,
    UnauthorizedError,
    ValidationError,
}

public record RotateApiKeySecretResult(
    RotateApiKeySecretResultCode ResultCode,
    string Message,
    string? Key
);
