using Corely.IAM.Security.Entities;

namespace Corely.IAM.Users.Entities;

internal class UserSymmetricKeyEntity : SymmetricKeyEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
}
