namespace Corely.IAM.BasicAuths.Models;

public enum CreateBasicAuthResultCode
{
    Success,
    BasicAuthExistsError,
    PasswordValidationError,
    ValidationError,
}

internal record CreateBasicAuthResult(
    CreateBasicAuthResultCode ResultCode,
    string? Message,
    Guid CreatedId
);
