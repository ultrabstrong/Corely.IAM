using Corely.DataAccess.Interfaces.Entities;

namespace Corely.IAM.Users.Entities;

internal class UserAuthTokenEntity : IHasCreatedUtc
{
    public const int REFRESH_SECRET_HASH_MAX_LENGTH = 250;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? AccountId { get; set; }
    public string DeviceId { get; set; } = null!;
    public Guid SessionId { get; set; }
    public DateTime SessionStartedUtc { get; set; }
    public DateTime IssuedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public string RefreshSecretHash { get; set; } = null!;
    public DateTime RefreshExpiresUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public virtual UserEntity? User { get; set; }
}
