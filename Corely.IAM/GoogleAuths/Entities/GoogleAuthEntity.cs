using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.GoogleAuths.Entities;

internal class GoogleAuthEntity : IHasCreatedUtc, IHasModifiedUtc
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string GoogleSubjectId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public virtual UserEntity User { get; set; } = null!;
}
