using Corely.IAM.Security.Providers;
using Corely.Security.KeyStore;
using Microsoft.Extensions.Configuration;

namespace Corely.IAM.Web.Security;

public class SecurityConfigurationProvider(IConfiguration configuration)
    : ISecurityConfigurationProvider
{
    private readonly InMemorySymmetricKeyStoreProvider _keyStoreProvider = new(
        configuration["Security:SystemKey"]
            ?? throw new InvalidOperationException("Security:SystemKey not found in configuration")
    );

    public ISymmetricKeyStoreProvider GetSystemSymmetricKey() => _keyStoreProvider;
}
