using Corely.Security.Keys;
using Corely.Security.KeyStore;

namespace Corely.IAM.UnitTests;

internal class SecurityConfigurationProvider : ISecurityConfigurationProvider
{
    private readonly string _symmetricKey;

    public SecurityConfigurationProvider()
    {
        _symmetricKey = new AesKeyProvider().CreateKey();
    }

    public ISymmetricKeyStoreProvider GetSystemSymmetricKey() =>
        new InMemorySymmetricKeyStoreProvider(_symmetricKey);
}
