using Corely.IAM.Invitations.Models;

namespace Corely.IAM.Web.Endpoints;

internal sealed record InvitationResponse(
    Guid Id,
    string Email,
    string? Description,
    DateTime CreatedUtc,
    DateTime ExpiresUtc,
    DateTime? AcceptedUtc,
    DateTime? RevokedUtc
)
{
    public static InvitationResponse From(Invitation invitation) =>
        new(
            invitation.Id,
            invitation.Email,
            invitation.Description,
            invitation.CreatedUtc,
            invitation.ExpiresUtc,
            invitation.AcceptedUtc,
            invitation.RevokedUtc
        );
}
