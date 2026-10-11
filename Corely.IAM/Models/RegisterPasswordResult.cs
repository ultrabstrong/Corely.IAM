namespace Corely.IAM.Models;

public enum RegisterPasswordResultCode
{
    Success,
    BasicAuthExistsError,
    TotpRequiredError,
    InvalidTotpCodeError,
    PasswordValidationError,
    ValidationError,
    UnauthorizedError,
}

public record RegisterPasswordResult(RegisterPasswordResultCode ResultCode, string Message);
