using Corely.IAM.Models;

namespace Corely.IAM.BasicAuths.Models.Extensions;

internal static class DeleteBasicAuthResultCodeExtensions
{
    extension(DeleteBasicAuthResultCode resultCode)
    {
        public DeregisterBasicAuthResultCode ToDeregisterBasicAuthResultCode() =>
            resultCode switch
            {
                DeleteBasicAuthResultCode.Success => DeregisterBasicAuthResultCode.Success,
                DeleteBasicAuthResultCode.NotFoundError =>
                    DeregisterBasicAuthResultCode.NotFoundError,
                DeleteBasicAuthResultCode.LastAuthMethodError =>
                    DeregisterBasicAuthResultCode.LastAuthMethodError,
                DeleteBasicAuthResultCode.UnauthorizedError =>
                    DeregisterBasicAuthResultCode.UnauthorizedError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(DeleteBasicAuthResultCode)}: {resultCode}"
                ),
            };
    }
}
