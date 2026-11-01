namespace Corely.IAM.Security.Models;

public interface IIamSymmetricEncryptionProvider
{
    string ProviderName { get; }
    string ProviderDescription { get; }
    int Version { get; }
    IReadOnlyList<int> CompromisedVersions { get; }
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string ReEncrypt(string ciphertext);
}
