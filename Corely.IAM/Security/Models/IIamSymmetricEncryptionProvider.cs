namespace Corely.IAM.Security.Models;

public interface IIamSymmetricEncryptionProvider
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string ReEncrypt(string ciphertext);
}
