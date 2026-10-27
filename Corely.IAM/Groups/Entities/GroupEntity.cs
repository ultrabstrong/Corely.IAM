using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.ApiKeys.Entities;
using Corely.IAM.Entities;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.Groups.Entities;

internal class GroupEntity : IHasCreatedUtc, IHasModifiedUtc, IAccountOwnedEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public Guid AccountId { get; set; }
    public virtual AccountEntity? Account { get; set; } = null!;
    public virtual ICollection<UserEntity>? Users { get; set; }
    public virtual ICollection<RoleEntity>? Roles { get; set; }
    public virtual ICollection<ApiKeyEntity>? ApiKeys { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
