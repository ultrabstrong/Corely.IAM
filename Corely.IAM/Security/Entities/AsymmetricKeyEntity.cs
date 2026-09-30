using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.Security.Enums;

namespace Corely.IAM.Security.Entities;

internal class AsymmetricKeyEntity : IKeyEntity, IHasCreatedUtc, IHasModifiedUtc
{
    public KeyUsedFor KeyUsedFor { get; set; }
    public string ProviderName { get; set; } = null!;
    public int Version { get; set; } = 1;
    public string PublicKey { get; set; } = null!;
    public string EncryptedPrivateKey { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
