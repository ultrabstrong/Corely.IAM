namespace Corely.IAM.Models;

public record DeregisterApiKeyResult(DeregisterApiKeyResultCode ResultCode, string Message);

public enum DeregisterApiKeyResultCode
{
    Success,
    ApiKeyNotFoundError,
    UnauthorizedError,
}
