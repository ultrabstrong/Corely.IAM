using Corely.IAM.Security.Entities;

namespace Corely.IAM.Accounts.Entities;

internal class AccountSymmetricKeyEntity : SymmetricKeyEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
}
