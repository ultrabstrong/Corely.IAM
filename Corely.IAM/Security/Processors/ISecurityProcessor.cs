using Corely.IAM.Security.Models;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.Security.Processors;

internal interface ISecurityProcessor
{
    SymmetricKey GetSymmetricEncryptionKeyEncryptedWithSystemKey();
    AsymmetricKey GetAsymmetricEncryptionKeyEncryptedWithSystemKey();
    AsymmetricKey GetAsymmetricSignatureKeyEncryptedWithSystemKey();
    string DecryptWithSystemKey(string encryptedValue);
    SigningCredentials GetAsymmetricSigningCredentials(
        string providerTypeCode,
        string key,
        bool isKeyPrivate
    );
}
