using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.MfaChallenges.Entities;

internal class MfaChallengeEntity : IHasCreatedUtc
{
    public const int CHALLENGE_TOKEN_BYTES = 32;
    public const int CHALLENGE_TOKEN_MAX_LENGTH = 128;
    public const int DEVICE_ID_MAX_LENGTH = 100;
    public const int MAX_ATTEMPTS = 5;

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
