using Corely.IAM.Security.Entities;

namespace Corely.IAM.Users.Entities;

internal class UserAsymmetricKeyEntity : AsymmetricKeyEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
}
