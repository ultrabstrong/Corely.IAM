namespace Corely.IAM.Models;

public enum SetPasswordResultCode
{
    Success,
    BasicAuthExistsError,
    PasswordValidationError,
    ValidationError,
    UnauthorizedError,
}

public record SetPasswordResult(SetPasswordResultCode ResultCode, string Message);
