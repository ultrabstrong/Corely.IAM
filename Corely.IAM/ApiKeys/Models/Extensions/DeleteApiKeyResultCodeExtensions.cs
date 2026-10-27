using Corely.IAM.Models;

namespace Corely.IAM.ApiKeys.Models.Extensions;

internal static class DeleteApiKeyResultCodeExtensions
{
    extension(DeleteApiKeyResultCode resultCode)
    {
        public DeregisterApiKeyResultCode ToDeregisterApiKeyResultCode() =>
            resultCode switch
            {
                DeleteApiKeyResultCode.Success => DeregisterApiKeyResultCode.Success,
                DeleteApiKeyResultCode.ApiKeyNotFoundError =>
                    DeregisterApiKeyResultCode.ApiKeyNotFoundError,
                DeleteApiKeyResultCode.UnauthorizedError =>
                    DeregisterApiKeyResultCode.UnauthorizedError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(DeleteApiKeyResultCode)}: {resultCode}"
                ),
            };
    }
}
