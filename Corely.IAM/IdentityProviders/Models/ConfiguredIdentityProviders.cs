using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.Security.Models;
using Microsoft.Extensions.Configuration;

namespace Corely.IAM.IdentityProviders.Models;

internal sealed record ConfiguredIdentityProviders(
    IReadOnlyList<StoredIdentityProvider> Providers,
    IReadOnlyList<string> ConfiguredKeys
)
{
    public const string SECTION = "IdentityProviders";

    public static ConfiguredIdentityProviders From(IConfiguration configuration)
    {
        var section = configuration.GetSection(SecurityOptions.NAME).GetSection(SECTION);
        var providers = new List<StoredIdentityProvider>();
        foreach (var child in section.GetChildren())
        {
            if (!Enum.TryParse<IdentityProviderType>(child.Key, true, out var type))
            {
                throw new InvalidOperationException(
                    $"{child.Path} names no identity provider. Use one of: {string.Join(", ", Enum.GetNames<IdentityProviderType>())}"
                );
            }

            var options = new IdentityProviderOptions();
            child.Bind(options);
            providers.Add(ToStored(type, options, child.Path));
        }

        return new(providers, [.. section.GetChildren().Select(c => c.Path)]);
    }

    private static StoredIdentityProvider ToStored(
        IdentityProviderType type,
        IdentityProviderOptions options,
        string path
    )
    {
        IdentityProviderCertificate? certificate = null;
        if (!string.IsNullOrWhiteSpace(options.CertificatePfxBase64))
        {
            try
            {
                certificate = IdentityProviderCertificate.FromPfx(
                    Convert.FromBase64String(options.CertificatePfxBase64),
                    options.CertificatePassword
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"{path}:CertificatePfxBase64 could not be read as a PFX with its private key",
                    ex
                );
            }
        }

        var credentialKind = certificate is null
            ? IdentityProviderCredentialKind.ClientSecret
            : IdentityProviderCredentialKind.Certificate;
        var credential = certificate?.Pem ?? options.ClientSecret;

        return new StoredIdentityProvider(
            new IdentityProviderSettings(
                type,
                options.Enabled,
                options.AllowSignUp,
                options.ClientId ?? string.Empty,
                credentialKind,
                !string.IsNullOrWhiteSpace(credential),
                certificate?.Thumbprint,
                options.Tenant
            ),
            null,
            credential
        );
    }
}
