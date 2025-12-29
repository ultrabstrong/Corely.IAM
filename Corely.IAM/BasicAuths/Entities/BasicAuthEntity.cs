using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.BasicAuths.Entities;

internal class BasicAuthEntity : IHasCreatedUtc, IHasModifiedUtc
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Password { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public UserEntity User { get; set; } = null!;
}
