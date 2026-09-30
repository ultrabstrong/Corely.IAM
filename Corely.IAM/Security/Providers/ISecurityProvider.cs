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
        IReadOnlyList<SymmetricKey> generations
    );
    IIamAsymmetricEncryptionProvider BuildAsymmetricEncryptionProvider(
        IReadOnlyList<AsymmetricKey> generations
    );
    IIamAsymmetricSignatureProvider BuildAsymmetricSignatureProvider(
        IReadOnlyList<AsymmetricKey> generations
    );
}
