using Corely.IAM.Security.Models;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.Security.Providers;

internal interface ISecurityProvider
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
