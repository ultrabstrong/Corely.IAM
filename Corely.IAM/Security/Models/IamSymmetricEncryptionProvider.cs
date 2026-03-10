using Corely.Security.Encryption.Providers;
using Corely.Security.KeyStore;

namespace Corely.IAM.Security.Models;

public class IamSymmetricEncryptionProvider(
    ISymmetricEncryptionProvider provider,
    ISymmetricKeyStoreProvider keyStore,
    string providerName
) : IIamSymmetricEncryptionProvider
{
    public string ProviderName => providerName;
    public string ProviderDescription => provider.ProviderDescription;

    public string Encrypt(string plaintext) => provider.Encrypt(plaintext, keyStore);

    public string Decrypt(string ciphertext) => provider.Decrypt(ciphertext, keyStore);

    public string ReEncrypt(string ciphertext) => provider.ReEncrypt(ciphertext, keyStore);
}
