using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Entities;

namespace Corely.IAM.Invitations.Entities;

internal class InvitationEntity : IHasCreatedUtc, IHasModifiedUtc, IAccountOwnedEntity
{
    public const int SECRET_HASH_MAX_LENGTH = 250;

    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string SecretHash { get; set; } = null!;
    public Guid CreatedByUserId { get; set; }
    public string Email { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public Guid? AcceptedByUserId { get; set; }
    public DateTime? AcceptedUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public virtual AccountEntity? Account { get; set; }
}
