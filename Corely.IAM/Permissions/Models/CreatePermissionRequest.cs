namespace Corely.IAM.Permissions.Models;

internal record CreatePermissionRequest(
    string PermissionName,
    int OwnerAccountId,
    string ResourceType,
    int ResourceId
);
