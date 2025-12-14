using Corely.Security.KeyStore;

namespace Corely.IAM;

public interface ISecurityConfigurationProvider
{
    public ISymmetricKeyStoreProvider GetSystemSymmetricKey();
}
