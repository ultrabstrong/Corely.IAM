namespace Corely.IAM.Permissions.Models;

public enum CreatePermissionResultCode
{
    Success,
    PermissionExistsError,
    AccountNotFoundError,
}

internal record CreatePermissionResult(
    CreatePermissionResultCode ResultCode,
    string Message,
    int CreatedId
);
