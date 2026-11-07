using Corely.IAM.IdentityProviders.Enums;

namespace Corely.IAM.IdentityProviders.Models;

public sealed class UpdateIdentityProviderRequest(
    IdentityProviderType type,
    bool enabled,
    bool allowSignUp,
    string clientId,
    IdentityProviderCredentialKind credentialKind,
    string? tenant = null,
    string? clientSecret = null,
    byte[]? certificatePfx = null,
    string? certificatePassword = null
)
{
    public IdentityProviderType Type { get; } = type;
    public bool Enabled { get; } = enabled;
    public bool AllowSignUp { get; } = allowSignUp;
    public string ClientId { get; } = clientId;
    public IdentityProviderCredentialKind CredentialKind { get; } = credentialKind;
    public string? Tenant { get; } = tenant;

    internal string? ClientSecret { get; } = clientSecret;
    internal byte[]? CertificatePfx { get; } = certificatePfx;
    internal string? CertificatePassword { get; } = certificatePassword;

    internal bool ReplacesCredential =>
        !string.IsNullOrWhiteSpace(ClientSecret) || CertificatePfx is { Length: > 0 };
}
