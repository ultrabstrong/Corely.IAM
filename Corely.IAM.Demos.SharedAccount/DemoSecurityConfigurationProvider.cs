using Corely.IAM.Security.Providers;
using Corely.Security.KeyStore;

namespace Corely.IAM.Demos.SharedAccount;

internal class DemoSecurityConfigurationProvider(IConfiguration configuration)
    : ISecurityConfigurationProvider
{
    private readonly InMemorySymmetricKeyStoreProvider _keyStoreProvider = new(
        configuration["Security:SystemKey"]
            ?? throw new InvalidOperationException("Security:SystemKey is not configured")
    );

    public ISymmetricKeyStoreProvider GetSystemSymmetricKey() => _keyStoreProvider;
}
