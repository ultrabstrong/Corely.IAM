namespace Corely.IAM.Models;

public enum SignInResultCode
{
    Success,
    UserNotFoundError,
    UserLockedError,
    PasswordMismatchError,
}

public record SignInResult(SignInResultCode ResultCode, string? Message, string? AuthToken);
