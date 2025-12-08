using Corely.DataAccess.Interfaces.Entities;

namespace Corely.IAM.Users.Entities;

internal class UserAuthTokenEntity : IHasIdPk<int>, IHasCreatedUtc
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? AccountId { get; set; }
    public string Jti { get; set; } = null!;
    public DateTime IssuedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public virtual UserEntity? User { get; set; }
}
