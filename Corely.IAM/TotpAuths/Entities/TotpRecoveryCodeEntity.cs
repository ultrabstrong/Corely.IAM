using Corely.DataAccess.Interfaces.Entities;

namespace Corely.IAM.TotpAuths.Entities;

internal class TotpRecoveryCodeEntity : IHasCreatedUtc
{
    public Guid Id { get; set; }
    public Guid TotpAuthId { get; set; }
    public string CodeHash { get; set; } = null!;
    public DateTime? UsedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public virtual TotpAuthEntity TotpAuth { get; set; } = null!;
}
