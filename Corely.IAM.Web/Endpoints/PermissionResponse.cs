using Corely.IAM.Permissions.Models;

namespace Corely.IAM.Web.Endpoints;

internal sealed record PermissionResponse(
    Guid Id,
    string ResourceType,
    Guid ResourceId,
    bool Create,
    bool Read,
    bool Update,
    bool Delete,
    bool Execute,
    string? Description,
    bool IsSystemDefined,
    IReadOnlyList<ReferenceResponse> Roles
)
{
    public static PermissionResponse From(Permission permission) =>
        new(
            permission.Id,
            permission.ResourceType,
            permission.ResourceId,
            permission.Create,
            permission.Read,
            permission.Update,
            permission.Delete,
            permission.Execute,
            permission.Description,
            permission.IsSystemDefined,
            ReferenceResponse.From(permission.Roles)
        );
}
