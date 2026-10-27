using Corely.IAM.Models;

namespace Corely.IAM.Groups.Models.Extensions;

internal static class RemoveApiKeysFromGroupResultCodeExtensions
{
    extension(RemoveApiKeysFromGroupResultCode resultCode)
    {
        public DeregisterApiKeysFromGroupResultCode ToDeregisterApiKeysFromGroupResultCode() =>
            resultCode switch
            {
                RemoveApiKeysFromGroupResultCode.Success =>
                    DeregisterApiKeysFromGroupResultCode.Success,
                RemoveApiKeysFromGroupResultCode.PartialSuccess =>
                    DeregisterApiKeysFromGroupResultCode.PartialSuccess,
                RemoveApiKeysFromGroupResultCode.InvalidApiKeyIdsError =>
                    DeregisterApiKeysFromGroupResultCode.InvalidApiKeyIdsError,
                RemoveApiKeysFromGroupResultCode.GroupNotFoundError =>
                    DeregisterApiKeysFromGroupResultCode.GroupNotFoundError,
                RemoveApiKeysFromGroupResultCode.UnauthorizedError =>
                    DeregisterApiKeysFromGroupResultCode.UnauthorizedError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(RemoveApiKeysFromGroupResultCode)}: {resultCode}"
                ),
            };
    }
}
