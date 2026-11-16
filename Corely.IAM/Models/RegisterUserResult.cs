namespace Corely.IAM.Models;

public enum RegisterUserResultCode
{
    Success,
    UserCreationError,
    BasicAuthCreationError,
    UnauthorizedError,
}

public record RegisterUserResult(
    RegisterUserResultCode ResultCode,
    string Message,
    Guid CreatedUserId
);
