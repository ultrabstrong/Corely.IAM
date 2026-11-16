namespace Corely.IAM.Models;

public enum RegisterAccountResultCode
{
    Success,
    AccountCreationError,
    AccountExistsError,
    SystemRoleAssignmentError,
    UnauthorizedError,
    EmailNotVerifiedError,
}

public record RegisterAccountResult(
    RegisterAccountResultCode ResultCode,
    string Message,
    Guid CreatedAccountId
);
