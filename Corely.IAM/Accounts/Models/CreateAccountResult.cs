namespace Corely.IAM.Accounts.Models;

public enum CreateAccountResultCode
{
    Success,
    AccountExistsError,
    UserOwnerNotFoundError,
}

internal record CreateAccountResult(
    CreateAccountResultCode ResultCode,
    string Message,
    int CreatedId
);
