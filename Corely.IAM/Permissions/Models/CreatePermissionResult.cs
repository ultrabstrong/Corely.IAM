namespace Corely.IAM.Permissions.Models;

public enum CreatePermissionResultCode
{
    Success,
    PermissionExistsError,
    AccountNotFoundError,
    UnauthorizedError,
    ValidationError,
}

internal record CreatePermissionResult(
    CreatePermissionResultCode ResultCode,
    string Message,
    Guid CreatedId
);
