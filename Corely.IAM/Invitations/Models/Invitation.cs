namespace Corely.IAM.Invitations.Models;

public class Invitation
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string Email { get; init; } = null!;
    public string? Description { get; init; }
    public DateTime ExpiresUtc { get; init; }
    public Guid? AcceptedByUserId { get; init; }
    public DateTime? AcceptedUtc { get; init; }
    public DateTime? RevokedUtc { get; init; }
    public DateTime CreatedUtc { get; init; }

    public bool IsExpired(DateTime utcNow) => ExpiresUtc < utcNow;

    public bool IsRevoked => RevokedUtc != null;
    public bool IsAccepted => AcceptedByUserId != null;

    public bool IsPending(DateTime utcNow) => !IsExpired(utcNow) && !IsRevoked && !IsAccepted;
}
