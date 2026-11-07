namespace Corely.IAM.IdentityProviders.Models;

public class IdentityProviderOptions
{
    public bool Enabled { get; set; } = true;
    public bool AllowSignUp { get; set; } = IdentityProviderSettings.ALLOW_SIGN_UP_DEFAULT;
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? CertificatePfxBase64 { get; set; }
    public string? CertificatePassword { get; set; }
    public string? Tenant { get; set; }
}
