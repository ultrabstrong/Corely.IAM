using Corely.IAM.Security.Providers;
using Corely.Security.KeyStore;

namespace Corely.IAM.DataAccessMigrations.Cli.Iam;

internal class SystemKeyConfigurationProvider(string systemKey) : ISecurityConfigurationProvider
{
    private readonly InMemorySymmetricKeyStoreProvider _keyStoreProvider = new(systemKey);

    public ISymmetricKeyStoreProvider GetSystemSymmetricKey() => _keyStoreProvider;
}
