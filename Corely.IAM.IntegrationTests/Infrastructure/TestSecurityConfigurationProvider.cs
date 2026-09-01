using System.Security.Cryptography;
using Corely.IAM.Security.Providers;
using Corely.Security.KeyStore;

namespace Corely.IAM.IntegrationTests.Infrastructure;

internal sealed class TestSecurityConfigurationProvider : ISecurityConfigurationProvider
{
    private readonly string _symmetricKey = CreateKey();

    public ISymmetricKeyStoreProvider GetSystemSymmetricKey() =>
        new InMemorySymmetricKeyStoreProvider(_symmetricKey);

    private static string CreateKey()
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        return Convert.ToBase64String(aes.Key);
    }
}
