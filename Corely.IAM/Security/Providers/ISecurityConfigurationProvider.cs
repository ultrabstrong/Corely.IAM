using Corely.Security.KeyStore;

namespace Corely.IAM.Security.Providers;

public interface ISecurityConfigurationProvider
{
    public ISymmetricKeyStoreProvider GetSystemSymmetricKey();
}
