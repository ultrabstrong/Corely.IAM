using Corely.IAM.Models;

namespace Corely.IAM.Accounts.Models.Extensions;

internal static class DeleteAccountResultCodeExtensions
{
    public static DeregisterAccountResultCode ToDeregisterAccountResultCode(
        this DeleteAccountResultCode resultCode
    ) =>
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
