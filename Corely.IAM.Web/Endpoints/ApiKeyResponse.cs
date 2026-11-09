using Corely.IAM.ApiKeys.Models;

namespace Corely.IAM.Web.Endpoints;

internal sealed record ApiKeyResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime? LockedUtc,
    DateTime? LastSuccessfulLoginUtc,
    DateTime CreatedUtc,
    IReadOnlyList<ReferenceResponse> Groups,
    IReadOnlyList<ReferenceResponse> Roles
)
{
    public static ApiKeyResponse From(ApiKey apiKey) =>
        new(
            apiKey.Id,
            apiKey.Name,
            apiKey.Description,
            apiKey.LockedUtc,
            apiKey.LastSuccessfulLoginUtc,
            apiKey.CreatedUtc,
            ReferenceResponse.From(apiKey.Groups),
            ReferenceResponse.From(apiKey.Roles)
        );
}
