namespace Corely.IAM.Accounts.Models;

internal record CloseAccountResult(CloseAccountResultCode ResultCode, string Message);

internal enum CloseAccountResultCode
{
    Success,
    AccountNotFoundError,
    UnauthorizedError,
    PlatformAccountError,
}
