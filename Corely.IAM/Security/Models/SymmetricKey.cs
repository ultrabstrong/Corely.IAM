using Corely.IAM.Security.Enums;
using Corely.Security.Encryption.Models;

namespace Corely.IAM.Security.Models;

public class SymmetricKey
{
    public Guid Id { get; set; }
    public KeyUsedFor KeyUsedFor { get; set; }
    public string ProviderTypeCode { get; set; } = null!;
    public int Version { get; set; }
    public ISymmetricEncryptedValue Key { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
