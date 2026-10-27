namespace Corely.IAM.ApiKeys.Models;

internal record DeleteApiKeyResult(DeleteApiKeyResultCode ResultCode, string Message);

internal enum DeleteApiKeyResultCode
{
    Success,
    ApiKeyNotFoundError,
    UnauthorizedError,
}
