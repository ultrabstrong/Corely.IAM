namespace Corely.IAM.PasswordRecoveries.Models;

public enum ValidatePasswordRecoveryTokenResultCode
{
    Success,
    PasswordRecoveryNotFoundError,
    PasswordRecoveryExpiredError,
    PasswordRecoveryAlreadyUsedError,
    PasswordRecoveryInvalidatedError,
}

public record ValidatePasswordRecoveryTokenResult(
    ValidatePasswordRecoveryTokenResultCode ResultCode,
    string Message
);
