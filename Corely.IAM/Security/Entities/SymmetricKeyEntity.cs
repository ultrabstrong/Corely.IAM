using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Security.Enums;

namespace Corely.IAM.Security.Entities;

internal class SymmetricKeyEntity : IHasCreatedUtc, IHasModifiedUtc
{
    public KeyUsedFor KeyUsedFor { get; set; }
    public string ProviderTypeCode { get; set; } = null!;
    public int Version { get; set; }
    public string EncryptedKey { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
