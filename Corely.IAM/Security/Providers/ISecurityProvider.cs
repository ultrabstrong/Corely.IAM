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
        string providerName,
        string key,
        bool isKeyPrivate
    );
    IIamSymmetricEncryptionProvider BuildSymmetricEncryptionProvider(
        IReadOnlyList<SymmetricKey> versions
    );
    IIamAsymmetricEncryptionProvider BuildAsymmetricEncryptionProvider(
        IReadOnlyList<AsymmetricKey> versions
    );
    IIamAsymmetricSignatureProvider BuildAsymmetricSignatureProvider(
        IReadOnlyList<AsymmetricKey> versions
    );
}
