using Corely.Security.KeyStore;
using Corely.Security.Signature.Providers;

namespace Corely.IAM.Security.Models;

public class IamAsymmetricSignatureProvider(
    IAsymmetricSignatureProvider provider,
    IAsymmetricKeyStoreProvider keyStore,
    string providerName,
    string publicKey,
    IReadOnlyList<(
        IAsymmetricSignatureProvider Provider,
        IAsymmetricKeyStoreProvider KeyStore
    )>? previousVersions = null,
    int version = 1
) : IIamAsymmetricSignatureProvider
{
    public string ProviderName => providerName;
    public string ProviderDescription => provider.ProviderDescription;
    public int Version => version;

    public string PublicKey => publicKey;

    public string Sign(string payload) => provider.Sign(payload, keyStore);

    public bool Verify(string payload, string signature) =>
        provider.Verify(payload, signature, keyStore)
        || (previousVersions?.Any(g => g.Provider.Verify(payload, signature, g.KeyStore)) ?? false);
}
