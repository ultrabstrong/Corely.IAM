namespace Corely.IAM.Web.Endpoints;

internal sealed record PermissionBody(
    string ResourceType,
    Guid? ResourceId = null,
    bool Create = false,
    bool Read = false,
    bool Update = false,
    bool Delete = false,
    bool Execute = false,
    string? Description = null
);
