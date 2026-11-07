using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.IdentityProviders.Enums;

namespace Corely.IAM.IdentityProviders.Entities;

internal class IdentityProviderEntity : IHasCreatedUtc, IHasModifiedUtc
{
    public IdentityProviderType Type { get; set; }
    public bool Enabled { get; set; }
    public bool AllowSignUp { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public IdentityProviderCredentialKind CredentialKind { get; set; }
    public string? EncryptedCredential { get; set; }
    public string? CertificateThumbprint { get; set; }
    public string? Tenant { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
