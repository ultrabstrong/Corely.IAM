namespace Corely.IAM.PasswordRecoveries.Models;

public enum RequestPasswordRecoveryResultCode
{
    Success,
    UserNotFoundError,
    ValidationError,
}

public record RequestPasswordRecoveryResult(
    RequestPasswordRecoveryResultCode ResultCode,
    string Message,
    string? RecoveryToken
);
