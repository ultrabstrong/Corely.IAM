namespace Corely.IAM.Web.Endpoints;

internal sealed record ApiKeyBody(
    string Name,
    string? Description = null,
    DateTime? ExpiresUtc = null
);
