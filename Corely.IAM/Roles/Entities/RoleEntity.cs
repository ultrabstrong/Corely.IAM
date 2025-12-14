using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.Roles.Entities;

internal class RoleEntity : IHasIdPk<int>, IHasCreatedUtc, IHasModifiedUtc
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public bool IsSystemDefined { get; set; }
    public int AccountId { get; set; }
    public virtual AccountEntity? Account { get; set; } = null!;
    public virtual ICollection<UserEntity>? Users { get; set; }
    public virtual ICollection<GroupEntity>? Groups { get; set; }
    public virtual ICollection<PermissionEntity>? Permissions { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
