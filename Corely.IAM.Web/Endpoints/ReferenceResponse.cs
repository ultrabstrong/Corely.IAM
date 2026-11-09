using Corely.IAM.Models;

namespace Corely.IAM.Web.Endpoints;

internal sealed record ReferenceResponse(Guid Id, string Name)
{
    public static IReadOnlyList<ReferenceResponse> From(IEnumerable<ChildRef>? refs) =>
        [.. (refs ?? []).Select(r => new ReferenceResponse(r.Id, r.Name))];
}
