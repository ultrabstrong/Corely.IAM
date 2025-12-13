namespace Corely.IAM.Accounts.Models;

public record DeleteAccountResult(DeleteAccountResultCode ResultCode, string Message);

public enum DeleteAccountResultCode
{
    Success,
    AccountNotFoundError,
    UnauthorizedError,
}
