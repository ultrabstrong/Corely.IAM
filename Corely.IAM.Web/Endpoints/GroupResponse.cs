using Corely.IAM.Groups.Models;

namespace Corely.IAM.Web.Endpoints;

internal sealed record GroupResponse(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<ReferenceResponse> Users,
    IReadOnlyList<ReferenceResponse> ApiKeys,
    IReadOnlyList<ReferenceResponse> Roles
)
{
    public static GroupResponse From(Group group) =>
        new(
            group.Id,
            group.Name,
            group.Description,
            ReferenceResponse.From(group.Users),
            ReferenceResponse.From(group.ApiKeys),
            ReferenceResponse.From(group.Roles)
        );
}
