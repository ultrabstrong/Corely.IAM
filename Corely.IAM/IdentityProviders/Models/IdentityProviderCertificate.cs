using System.Security.Cryptography.X509Certificates;

namespace Corely.IAM.IdentityProviders.Models;

internal sealed record IdentityProviderCertificate(string Pem, string Thumbprint)
{
    public static IdentityProviderCertificate FromPfx(byte[] pfx, string? password)
    {
        using var certificate = X509CertificateLoader.LoadPkcs12(
            pfx,
            password,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet
        );
        using var key =
            (System.Security.Cryptography.AsymmetricAlgorithm?)certificate.GetRSAPrivateKey()
            ?? certificate.GetECDsaPrivateKey()
            ?? throw new InvalidOperationException("The certificate carries no private key");
        var pem = certificate.ExportCertificatePem() + "\n" + key.ExportPkcs8PrivateKeyPem();
        return new(pem, certificate.Thumbprint);
    }
}
