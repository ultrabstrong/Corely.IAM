using Corely.IAM.Users.Models;

namespace Corely.IAM.Web.Endpoints;

internal sealed record UserResponse(
    Guid Id,
    string Username,
    string Email,
    DateTime? LockedUtc,
    DateTime? LastSuccessfulLoginUtc,
    DateTime CreatedUtc,
    IReadOnlyList<ReferenceResponse> Groups,
    IReadOnlyList<ReferenceResponse> Roles
)
{
    public static UserResponse From(User user) =>
        new(
            user.Id,
            user.Username,
            user.Email,
            user.LockedUtc,
            user.LastSuccessfulLoginUtc,
            user.CreatedUtc,
            ReferenceResponse.From(user.Groups),
            ReferenceResponse.From(user.Roles)
        );
}
