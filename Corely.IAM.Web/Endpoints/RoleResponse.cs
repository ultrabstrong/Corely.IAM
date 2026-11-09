using Corely.IAM.Roles.Models;

namespace Corely.IAM.Web.Endpoints;

internal sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemDefined,
    IReadOnlyList<ReferenceResponse> Users,
    IReadOnlyList<ReferenceResponse> Groups,
    IReadOnlyList<ReferenceResponse> ApiKeys,
    IReadOnlyList<ReferenceResponse> Permissions
)
{
    public static RoleResponse From(Role role) =>
        new(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemDefined,
            ReferenceResponse.From(role.Users),
            ReferenceResponse.From(role.Groups),
            ReferenceResponse.From(role.ApiKeys),
            ReferenceResponse.From(role.Permissions)
        );
}
