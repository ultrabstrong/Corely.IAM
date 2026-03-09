using Corely.Security.KeyStore;
using Corely.Security.Signature.Providers;

namespace Corely.IAM.Security.Models;

public class IamAsymmetricSignatureProvider(
    IAsymmetricSignatureProvider provider,
    IAsymmetricKeyStoreProvider keyStore
) : IIamAsymmetricSignatureProvider
{
    public string Sign(string payload) => provider.Sign(payload, keyStore);

    public bool Verify(string payload, string signature) =>
        provider.Verify(payload, signature, keyStore);
}
