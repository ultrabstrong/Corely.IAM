namespace Corely.IAM.Permissions.Models;

internal record CreatePermissionRequest(
    int OwnerAccountId,
    string ResourceType,
    int ResourceId,
    bool Create = false,
    bool Read = false,
    bool Update = false,
    bool Delete = false,
    bool Execute = false,
    string? Description = null
);
