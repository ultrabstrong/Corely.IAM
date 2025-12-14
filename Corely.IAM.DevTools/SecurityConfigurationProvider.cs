using Corely.IAM;
using Corely.Security.KeyStore;

namespace Corely.IAM.DevTools;

internal class SecurityConfigurationProvider(string symmetricKey) : ISecurityConfigurationProvider
{
    private readonly InMemorySymmetricKeyStoreProvider _keyStoreProvider = new(symmetricKey);

    public ISymmetricKeyStoreProvider GetSystemSymmetricKey() => _keyStoreProvider;
}
