using Corely.IAM.IdentityProviders.Enums;

namespace Corely.IAM.IdentityProviders.Models;

public record IdentityProviderSettings(
    IdentityProviderType Type,
    bool Enabled,
    bool AllowSignUp,
    string ClientId,
    IdentityProviderCredentialKind CredentialKind,
    bool HasCredential,
    string? CertificateThumbprint,
    string? Tenant
)
{
    public const int CLIENT_ID_MAX_LENGTH = 500;
    public const int TENANT_MAX_LENGTH = 100;
    public const int CERTIFICATE_THUMBPRINT_MAX_LENGTH = 128;
    public const bool ALLOW_SIGN_UP_DEFAULT = true;

    public bool IsUsable => Enabled && !string.IsNullOrWhiteSpace(ClientId) && HasCredential;
}
