namespace Corely.IAM.Models;

public record RegisterPermissionRequest(
    string PermissionName,
    int OwnerAccountId,
    string ResourceType,
    int ResourceId
);
