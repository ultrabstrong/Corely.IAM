namespace Corely.IAM.Models;

public enum ModifyPasswordResultCode
{
    Success,
    BasicAuthNotFoundError,
    InvalidCurrentPasswordError,
    UserLockedError,
    TotpRequiredError,
    InvalidTotpCodeError,
    PasswordValidationError,
    ValidationError,
    UnauthorizedError,
}

public record ModifyPasswordResult(ModifyPasswordResultCode ResultCode, string Message);
