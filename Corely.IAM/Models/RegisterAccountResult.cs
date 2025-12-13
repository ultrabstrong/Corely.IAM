namespace Corely.IAM.Models;

public enum RegisterAccountResultCode
{
    Success,
    AccountCreationError,
    SystemRoleAssignmentError,
}

public record RegisterAccountResult(
    RegisterAccountResultCode ResultCode,
    string? Message,
    Guid CreatedAccountId
);
