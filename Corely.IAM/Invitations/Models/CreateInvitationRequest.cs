namespace Corely.IAM.Invitations.Models;

public record CreateInvitationRequest(
    Guid AccountId,
    string Email,
    string? Description,
    int ExpiresInSeconds
)
{
    public const int MIN_EXPIRY_SECONDS = 300;
    public const int MAX_EXPIRY_SECONDS = 2_592_000;
    public const int DEFAULT_EXPIRY_SECONDS = 604_800;
}
