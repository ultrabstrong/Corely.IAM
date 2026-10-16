namespace Corely.IAM.BasicAuths.Models;

internal enum CreateBasicAuthResultCode
{
    Success,
    BasicAuthExistsError,
    PasswordValidationError,
    ValidationError,
}

internal record CreateBasicAuthResult(
    CreateBasicAuthResultCode ResultCode,
    string Message,
    Guid CreatedId
);
