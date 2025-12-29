namespace Corely.IAM.Models;

public enum RegisterUserResultCode
{
    Success,
    UserCreationError,
    BasicAuthCreationError,
}

public record RegisterUserResult(
    RegisterUserResultCode ResultCode,
    string? Message,
    Guid CreatedUserId
);
