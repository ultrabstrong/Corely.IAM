using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.ApiKeys.Enums;

namespace Corely.IAM.ApiKeys.Entities;

internal class ApiKeySecretEntity : IHasCreatedUtc
{
    public const int PREFIX_LENGTH = 8;
    public const int SECRET_HASH_MAX_LENGTH = 250;

    public Guid Id { get; set; }
    public Guid ApiKeyId { get; set; }
    public ApiKeySlot Slot { get; set; }
    public string Prefix { get; set; } = null!;
    public string SecretHash { get; set; } = null!;
    public DateTime? ExpiresUtc { get; set; }
    public DateTime? LastUsedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public virtual ApiKeyEntity? ApiKey { get; set; }
}
