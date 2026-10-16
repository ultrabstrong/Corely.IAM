namespace Corely.IAM.BasicAuths.Models;

internal enum UpdateBasicAuthResultCode
{
    Success,
    BasicAuthNotFoundError,
    UnauthorizedError,
    PasswordValidationError,
    ValidationError,
}

internal record UpdateBasicAuthResult(UpdateBasicAuthResultCode ResultCode, string Message);
