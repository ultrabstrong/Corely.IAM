namespace Corely.IAM.PasswordRecoveries.Models;

public enum ResetPasswordWithRecoveryResultCode
{
    Success,
    PasswordRecoveryNotFoundError,
    PasswordRecoveryExpiredError,
    PasswordRecoveryAlreadyUsedError,
    PasswordRecoveryInvalidatedError,
    PasswordValidationError,
    ValidationError,
}

public record ResetPasswordWithRecoveryResult(
    ResetPasswordWithRecoveryResultCode ResultCode,
    string Message
);
