using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.EmailVerifications.Entities;

internal class EmailVerificationChallengeEntity : IHasCreatedUtc
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SecretHash { get; set; } = null!;
    public string DeviceId { get; set; } = null!;
    public Guid? AccountId { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public virtual UserEntity User { get; set; } = null!;
}
