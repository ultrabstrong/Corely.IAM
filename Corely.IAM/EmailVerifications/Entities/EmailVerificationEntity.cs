using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.EmailVerifications.Entities;

internal class EmailVerificationEntity : IHasCreatedUtc
{
    public const int SECRET_HASH_MAX_LENGTH = 250;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string SecretHash { get; set; } = null!;
    public DateTime SentUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public DateTime? InvalidatedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public virtual UserEntity User { get; set; } = null!;
}
