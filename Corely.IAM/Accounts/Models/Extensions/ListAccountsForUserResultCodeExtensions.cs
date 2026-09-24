using Corely.IAM.Models;

namespace Corely.IAM.Accounts.Models.Extensions;

internal static class ListAccountsForUserResultCodeExtensions
{
    extension(ListAccountsForUserResultCode resultCode)
    {
        public RetrieveAccountsResultCode ToRetrieveAccountsResultCode() =>
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
}
