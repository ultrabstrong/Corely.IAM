namespace Corely.IAM.Models;

public record RegisterPermissionRequest(
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
