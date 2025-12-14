using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Security.Entities;

namespace Corely.IAM.Accounts.Entities;

internal class AccountAsymmetricKeyEntity : AsymmetricKeyEntity, IHasIdPk<int>
{
    public int Id { get; set; }
    public int AccountId { get; set; }
}
