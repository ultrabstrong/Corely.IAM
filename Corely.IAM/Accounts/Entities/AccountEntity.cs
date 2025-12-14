using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.Accounts.Entities;

internal class AccountEntity : IHasIdPk<int>, IHasCreatedUtc, IHasModifiedUtc
{
    public int Id { get; set; }
    public Guid PublicId { get; set; }
    public string AccountName { get; init; } = null!;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public virtual ICollection<UserEntity>? Users { get; set; }
    public virtual ICollection<GroupEntity>? Groups { get; set; }
    public virtual ICollection<RoleEntity>? Roles { get; set; }
    public virtual ICollection<PermissionEntity>? Permissions { get; set; }
    public virtual ICollection<AccountSymmetricKeyEntity>? SymmetricKeys { get; init; }
    public virtual ICollection<AccountAsymmetricKeyEntity>? AsymmetricKeys { get; init; }
}
