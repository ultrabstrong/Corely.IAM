namespace Corely.IAM.Security.Models;

public interface IIamAsymmetricEncryptionProvider
{
    string ProviderName { get; }
    string ProviderDescription { get; }
    string PublicKey { get; }
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string ReEncrypt(string ciphertext);
}
