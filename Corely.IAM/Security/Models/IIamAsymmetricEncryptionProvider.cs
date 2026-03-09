namespace Corely.IAM.Security.Models;

public interface IIamAsymmetricEncryptionProvider
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string ReEncrypt(string ciphertext);
}
