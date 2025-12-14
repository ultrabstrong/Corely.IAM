using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Security.Entities;

namespace Corely.IAM.Users.Entities;

internal class UserAsymmetricKeyEntity : AsymmetricKeyEntity, IHasIdPk<int>
{
    public int Id { get; set; }
    public int UserId { get; set; }
}
