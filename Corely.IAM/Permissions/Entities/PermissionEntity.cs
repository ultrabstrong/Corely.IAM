using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Roles.Entities;

namespace Corely.IAM.Permissions.Entities;

internal class PermissionEntity : IHasIdPk<int>, IHasCreatedUtc, IHasModifiedUtc
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public int AccountId { get; set; }
    public virtual AccountEntity? Account { get; set; } = null!;
    public virtual ICollection<RoleEntity>? Roles { get; set; }
    public string ResourceType { get; set; } = null!;
    public int ResourceId { get; set; }
    public bool Create { get; set; }
    public bool Read { get; set; }
    public bool Update { get; set; }
    public bool Delete { get; set; }
    public bool Execute { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
