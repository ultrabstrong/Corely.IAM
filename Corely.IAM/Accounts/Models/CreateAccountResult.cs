namespace Corely.IAM.Accounts.Models;

public enum CreateAccountResultCode
{
    Success,
    AccountExistsError,
    UserOwnerNotFoundError,
    ValidationError,
}

internal record CreateAccountResult(
    CreateAccountResultCode ResultCode,
    string Message,
    Guid CreatedId
);
