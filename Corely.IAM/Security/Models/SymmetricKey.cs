using Corely.IAM.Security.Enums;
using Corely.Security.Encryption.Models;

namespace Corely.IAM.Security.Models;

public class SymmetricKey
{
    public Guid Id { get; set; }
    public KeyUsedFor KeyUsedFor { get; set; }
    public string ProviderName { get; set; } = null!;
    public int Version { get; set; }
    public int Generation { get; set; } = 1;
    public ISymmetricEncryptedValue Key { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
