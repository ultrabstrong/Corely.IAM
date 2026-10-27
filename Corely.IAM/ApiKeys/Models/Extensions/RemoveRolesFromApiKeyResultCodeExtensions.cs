using Corely.IAM.Models;

namespace Corely.IAM.ApiKeys.Models.Extensions;

internal static class RemoveRolesFromApiKeyResultCodeExtensions
{
    extension(RemoveRolesFromApiKeyResultCode resultCode)
    {
        public DeregisterRolesFromApiKeyResultCode ToDeregisterRolesFromApiKeyResultCode() =>
            resultCode switch
            {
                RemoveRolesFromApiKeyResultCode.Success =>
                    DeregisterRolesFromApiKeyResultCode.Success,
                RemoveRolesFromApiKeyResultCode.PartialSuccess =>
                    DeregisterRolesFromApiKeyResultCode.PartialSuccess,
                RemoveRolesFromApiKeyResultCode.InvalidRoleIdsError =>
                    DeregisterRolesFromApiKeyResultCode.InvalidRoleIdsError,
                RemoveRolesFromApiKeyResultCode.ApiKeyNotFoundError =>
                    DeregisterRolesFromApiKeyResultCode.ApiKeyNotFoundError,
                RemoveRolesFromApiKeyResultCode.UnauthorizedError =>
                    DeregisterRolesFromApiKeyResultCode.UnauthorizedError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(RemoveRolesFromApiKeyResultCode)}: {resultCode}"
                ),
            };
    }
}
