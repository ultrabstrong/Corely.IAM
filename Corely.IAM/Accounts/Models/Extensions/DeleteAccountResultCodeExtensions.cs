using Corely.IAM.Models;

namespace Corely.IAM.Accounts.Models.Extensions;

internal static class DeleteAccountResultCodeExtensions
{
    extension(DeleteAccountResultCode resultCode)
    {
        public DeregisterAccountResultCode ToDeregisterAccountResultCode() =>
            resultCode switch
            {
                DeleteAccountResultCode.Success => DeregisterAccountResultCode.Success,
                DeleteAccountResultCode.AccountNotFoundError =>
                    DeregisterAccountResultCode.AccountNotFoundError,
                DeleteAccountResultCode.UnauthorizedError =>
                    DeregisterAccountResultCode.UnauthorizedError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(DeleteAccountResultCode)}: {resultCode}"
                ),
            };
    }
}
