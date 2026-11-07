using System.Security.Cryptography.X509Certificates;

namespace Corely.IAM.IdentityProviders.Models;

internal sealed record ResolvedIdentityProvider(
    IdentityProviderSettings Settings,
    string? ClientSecret,
    string? CertificatePem
)
{
    public X509Certificate2 LoadCertificate() =>
        X509Certificate2.CreateFromPem(CertificatePem, CertificatePem);
}
