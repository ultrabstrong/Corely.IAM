namespace Corely.IAM.Models;

public record RegisterPermissionRequest(
    Guid AccountId,
    string ResourceType,
    Guid ResourceId,
    bool Create = false,
    bool Read = false,
    bool Update = false,
    bool Delete = false,
    bool Execute = false,
    string? Description = null
);
