namespace Corely.IAM.Accounts.Models;

internal enum CreateAccountResultCode
{
    Success,
    AccountExistsError,
    UserOwnerNotFoundError,
    ValidationError,
    UnauthorizedError,
}

internal record CreateAccountResult(
    CreateAccountResultCode ResultCode,
    string Message,
    Guid CreatedId
);
