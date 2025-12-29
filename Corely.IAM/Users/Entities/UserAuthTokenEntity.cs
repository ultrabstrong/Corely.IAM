using Corely.DataAccess.Interfaces.Entities;

namespace Corely.IAM.Users.Entities;

internal class UserAuthTokenEntity : IHasCreatedUtc
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? AccountId { get; set; }
    public string DeviceId { get; set; } = null!;
    public DateTime IssuedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public virtual UserEntity? User { get; set; }
}
