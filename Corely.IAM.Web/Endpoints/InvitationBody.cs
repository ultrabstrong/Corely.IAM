namespace Corely.IAM.Web.Endpoints;

internal sealed record InvitationBody(
    string Email,
    string? Description = null,
    int? ExpiresInSeconds = null
);
