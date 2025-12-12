using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Models;

public enum SignInResultCode
{
    Success,
    UserNotFoundError,
    UserLockedError,
    PasswordMismatchError,
    SignatureKeyNotFoundError,
    AccountNotFoundError,
}

public record SignInResult(
    SignInResultCode ResultCode,
    string? Message,
    string? AuthToken,
    List<Account> Accounts,
    int? SignedInAccountId
);
