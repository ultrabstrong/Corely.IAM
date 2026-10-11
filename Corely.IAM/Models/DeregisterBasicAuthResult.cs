namespace Corely.IAM.Models;

public enum DeregisterBasicAuthResultCode
{
    Success,
    NotFoundError,
    LastAuthMethodError,
    InvalidCurrentPasswordError,
    UserLockedError,
    TotpRequiredError,
    InvalidTotpCodeError,
    UnauthorizedError,
}

public record DeregisterBasicAuthResult(DeregisterBasicAuthResultCode ResultCode, string Message);
