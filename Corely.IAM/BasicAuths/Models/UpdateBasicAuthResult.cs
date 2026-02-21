namespace Corely.IAM.BasicAuths.Models;

public enum UpdateBasicAuthResultCode
{
    Success,
    BasicAuthNotFoundError,
    UnauthorizedError,
    PasswordValidationError,
}

internal record UpdateBasicAuthResult(UpdateBasicAuthResultCode ResultCode, string? Message);
