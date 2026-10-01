using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Providers;
using Corely.Security.KeyStore;

namespace Corely.IAM.Security.Models;

public class IamSymmetricEncryptionProvider(
    ISymmetricEncryptionProvider provider,
    ISymmetricKeyStoreProvider keyStore,
    string providerName,
    ISymmetricEncryptionProviderFactory? decryptingProviders = null,
    int version = 1
) : IIamSymmetricEncryptionProvider
{
    public string ProviderName => providerName;
    public string ProviderDescription => provider.ProviderDescription;
    public int Version => version;

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

    private ISymmetricEncryptionProvider ProviderFor(string ciphertext) =>
        decryptingProviders?.GetProviderForDecrypting(ciphertext) ?? provider;
}
