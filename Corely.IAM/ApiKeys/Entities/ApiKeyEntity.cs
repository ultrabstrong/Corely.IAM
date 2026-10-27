using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Entities;

namespace Corely.IAM.ApiKeys.Entities;

internal class ApiKeyEntity : IHasCreatedUtc, IHasModifiedUtc, IAccountOwnedEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? LockedUtc { get; set; }
    public int TotalSuccessfulLogins { get; set; }
    public DateTime? LastSuccessfulLoginUtc { get; set; }
    public int FailedLoginsSinceLastSuccess { get; set; }
    public int TotalFailedLogins { get; set; }
    public DateTime? LastFailedLoginUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public virtual AccountEntity? Account { get; set; }
    public virtual ICollection<ApiKeySecretEntity>? Secrets { get; set; }
    public virtual ICollection<GroupEntity>? Groups { get; set; }
    public virtual ICollection<RoleEntity>? Roles { get; set; }
    public virtual ICollection<AuthTokenEntity>? AuthTokens { get; init; }
}
