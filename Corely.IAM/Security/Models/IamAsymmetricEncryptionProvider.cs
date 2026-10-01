using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Providers;
using Corely.Security.KeyStore;

namespace Corely.IAM.Security.Models;

public class IamAsymmetricEncryptionProvider(
    IAsymmetricEncryptionProvider provider,
    IAsymmetricKeyStoreProvider keyStore,
    string providerName,
    string publicKey,
    IAsymmetricEncryptionProviderFactory? decryptingProviders = null,
    int version = 1
) : IIamAsymmetricEncryptionProvider
{
    public string ProviderName => providerName;
    public string ProviderDescription => provider.ProviderDescription;
    public int Version => version;

    public string PublicKey => publicKey;

    public string Encrypt(string plaintext) => provider.Encrypt(plaintext, keyStore);

    public string Decrypt(string ciphertext) =>
        ProviderFor(ciphertext).Decrypt(ciphertext, keyStore);

    public string ReEncrypt(string ciphertext)
    {
        var source = ProviderFor(ciphertext);
        return source.ProviderName == provider.ProviderName
            ? provider.ReEncrypt(ciphertext, keyStore)
            : provider.Encrypt(source.Decrypt(ciphertext, keyStore), keyStore);
    }

    private IAsymmetricEncryptionProvider ProviderFor(string ciphertext) =>
        decryptingProviders?.GetProviderForDecrypting(ciphertext) ?? provider;
}
