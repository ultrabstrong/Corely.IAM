namespace Corely.IAM.BasicAuths.Models;

public enum CreateBasicAuthResultCode
{
    Success,
    BasicAuthExistsError,
    PasswordValidationError,
}

internal record CreateBasicAuthResult(
    CreateBasicAuthResultCode ResultCode,
    string? Message,
    Guid CreatedId
);
