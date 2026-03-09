using Corely.Security.Encryption.Providers;
using Corely.Security.KeyStore;

namespace Corely.IAM.Security.Models;

public class IamAsymmetricEncryptionProvider(
    IAsymmetricEncryptionProvider provider,
    IAsymmetricKeyStoreProvider keyStore
) : IIamAsymmetricEncryptionProvider
{
    public string Encrypt(string plaintext) => provider.Encrypt(plaintext, keyStore);

    public string Decrypt(string ciphertext) => provider.Decrypt(ciphertext, keyStore);

    public string ReEncrypt(string ciphertext) => provider.ReEncrypt(ciphertext, keyStore);
}
