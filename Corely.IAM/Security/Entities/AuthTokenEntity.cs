using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.ApiKeys.Entities;
using Corely.IAM.ApiKeys.Enums;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.Security.Entities;

internal class AuthTokenEntity : IHasCreatedUtc
{
    public const int REFRESH_SECRET_HASH_MAX_LENGTH = 250;

    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ApiKeyId { get; set; }
    public ApiKeySlot? Slot { get; set; }
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
    public virtual ApiKeyEntity? ApiKey { get; set; }
}
