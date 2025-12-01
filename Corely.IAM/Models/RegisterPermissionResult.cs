using Corely.IAM.Permissions.Models;

namespace Corely.IAM.Models;

public record RegisterPermissionResult(
    CreatePermissionResultCode ResultCode,
    string Message,
    int CreatedPermissionId
);
