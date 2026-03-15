using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.MfaChallenges.Entities;

internal class MfaChallengeEntity : IHasCreatedUtc
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ChallengeToken { get; set; } = null!;
    public string DeviceId { get; set; } = null!;
    public Guid? AccountId { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime CreatedUtc { get; set; }
    public virtual UserEntity? User { get; set; }
}
