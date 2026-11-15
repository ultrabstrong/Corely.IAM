using Corely.IAM.Models;

namespace Corely.IAM.Accounts.Models.Extensions;

internal static class CloseAccountResultCodeExtensions
{
    extension(CloseAccountResultCode resultCode)
    {
        public DeregisterAccountAccessResultCode ToDeregisterAccountAccessResultCode() =>
            resultCode switch
            {
                CloseAccountResultCode.Success => DeregisterAccountAccessResultCode.Success,
                CloseAccountResultCode.AccountNotFoundError =>
                    DeregisterAccountAccessResultCode.AccountNotFoundError,
                CloseAccountResultCode.UnauthorizedError =>
                    DeregisterAccountAccessResultCode.UnauthorizedError,
                CloseAccountResultCode.PlatformAccountError =>
                    DeregisterAccountAccessResultCode.PlatformAccountError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(CloseAccountResultCode)}: {resultCode}"
                ),
            };

        public DeregisterAccountResultCode ToDeregisterAccountResultCode() =>
            resultCode switch
            {
                CloseAccountResultCode.Success => DeregisterAccountResultCode.Success,
                CloseAccountResultCode.AccountNotFoundError =>
                    DeregisterAccountResultCode.AccountNotFoundError,
                CloseAccountResultCode.UnauthorizedError =>
                    DeregisterAccountResultCode.UnauthorizedError,
                CloseAccountResultCode.PlatformAccountError =>
                    DeregisterAccountResultCode.PlatformAccountError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(CloseAccountResultCode)}: {resultCode}"
                ),
            };
    }
}
