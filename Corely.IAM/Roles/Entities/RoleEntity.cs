using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.Roles.Entities;

internal class RoleEntity : IHasCreatedUtc, IHasModifiedUtc
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public bool IsSystemDefined { get; set; }
    public Guid AccountId { get; set; }
    public virtual AccountEntity? Account { get; set; } = null!;
    public virtual ICollection<UserEntity>? Users { get; set; }
    public virtual ICollection<GroupEntity>? Groups { get; set; }
    public virtual ICollection<PermissionEntity>? Permissions { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
