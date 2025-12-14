using Corely.IAM.Models;

namespace Corely.IAM.Accounts.Models.Extensions;

internal static class ListAccountsForUserResultCodeExtensions
{
    public static RetrieveAccountsResultCode ToRetrieveAccountsResultCode(
        this ListAccountsForUserResultCode resultCode
    ) =>
        resultCode switch
        {
            ListAccountsForUserResultCode.Success => RetrieveAccountsResultCode.Success,
            ListAccountsForUserResultCode.UnauthorizedError =>
                RetrieveAccountsResultCode.UnauthorizedError,
            _ => throw new ArgumentOutOfRangeException(
                nameof(resultCode),
                resultCode,
                $"Unmapped {nameof(ListAccountsForUserResultCode)}: {resultCode}"
            ),
        };
}
