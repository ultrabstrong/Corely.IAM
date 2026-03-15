using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.TotpAuths.Entities;

internal class TotpAuthEntity : IHasCreatedUtc, IHasModifiedUtc
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string EncryptedSecret { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public virtual UserEntity User { get; set; } = null!;
    public virtual ICollection<TotpRecoveryCodeEntity>? RecoveryCodes { get; set; }
}
