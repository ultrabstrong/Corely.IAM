using System.Security.Cryptography;
using Corely.IAM.Security.Providers;
using Corely.Security.KeyStore;

namespace Corely.IAM.IntegrationTests.Infrastructure;

/// <summary>
/// A throwaway system key per host. Matches the base64 AES key format the library's own key
/// provider emits; that provider is internal to Corely.Security so it cannot be called here.
/// </summary>
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
