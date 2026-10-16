namespace Corely.IAM.Accounts.Models;

internal record DeleteAccountResult(DeleteAccountResultCode ResultCode, string Message);

internal enum DeleteAccountResultCode
{
    Success,
    AccountNotFoundError,
    UnauthorizedError,
    PlatformAccountError,
}
