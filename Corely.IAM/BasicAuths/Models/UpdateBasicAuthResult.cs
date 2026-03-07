namespace Corely.IAM.BasicAuths.Models;

public enum UpdateBasicAuthResultCode
{
    Success,
    BasicAuthNotFoundError,
    UnauthorizedError,
    PasswordValidationError,
    ValidationError,
}

internal record UpdateBasicAuthResult(UpdateBasicAuthResultCode ResultCode, string Message);
