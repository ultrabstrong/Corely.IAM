using Corely.Common.Extensions;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Models;
using Corely.Security.Signature.Factories;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.Security.Providers;

internal class SecurityProvider(
    ISecurityConfigurationProvider securityConfigurationProvider,
    ISymmetricEncryptionProviderFactory symmetricEncryptionProviderFactory,
    IAsymmetricEncryptionProviderFactory asymmetricEncryptionProviderFactory,
    IAsymmetricSignatureProviderFactory asymmetricSignatureProviderFactory
) : ISecurityProvider
{
    private readonly ISecurityConfigurationProvider _securityConfigurationProvider =
        securityConfigurationProvider.ThrowIfNull(nameof(securityConfigurationProvider));
    private readonly ISymmetricEncryptionProviderFactory _symmetricEncryptionProviderFactory =
        symmetricEncryptionProviderFactory.ThrowIfNull(nameof(symmetricEncryptionProviderFactory));
    private readonly IAsymmetricEncryptionProviderFactory _asymmetricEncryptionProviderFactory =
        asymmetricEncryptionProviderFactory.ThrowIfNull(
            nameof(asymmetricEncryptionProviderFactory)
        );
    private readonly IAsymmetricSignatureProviderFactory _asymmetricSignatureProviderFactory =
        asymmetricSignatureProviderFactory.ThrowIfNull(nameof(asymmetricSignatureProviderFactory));

    public SymmetricKey GetSymmetricEncryptionKeyEncryptedWithSystemKey()
    {
        var systemKeyStoreProvider = _securityConfigurationProvider.GetSystemSymmetricKey();
        var symmetricEncryptionProvider = _symmetricEncryptionProviderFactory.GetDefaultProvider();

        var decryptedKey = symmetricEncryptionProvider.GetSymmetricKeyProvider().CreateKey();
        var encryptedKey = symmetricEncryptionProvider.Encrypt(
            decryptedKey,
            systemKeyStoreProvider
        );

        var symmetricKey = new SymmetricKey
        {
            KeyUsedFor = KeyUsedFor.Encryption,
            ProviderTypeCode = symmetricEncryptionProvider.EncryptionTypeCode,
            Version = systemKeyStoreProvider.GetCurrentVersion(),
            Key = new SymmetricEncryptedValue(symmetricEncryptionProvider)
            {
                Secret = encryptedKey,
            },
        };
        return symmetricKey;
    }

    public AsymmetricKey GetAsymmetricEncryptionKeyEncryptedWithSystemKey()
    {
        var systemKeyStoreProvider = _securityConfigurationProvider.GetSystemSymmetricKey();
        var asymmetricEncryptionProvider =
            _asymmetricEncryptionProviderFactory.GetDefaultProvider();
        var symmetricEncryptionProvider = _symmetricEncryptionProviderFactory.GetDefaultProvider();

        var (publickey, privateKey) = asymmetricEncryptionProvider
            .GetAsymmetricKeyProvider()
            .CreateKeys();
        var encryptedPrivateKey = symmetricEncryptionProvider.Encrypt(
            privateKey,
            systemKeyStoreProvider
        );

        var asymmetricKey = new AsymmetricKey
        {
            KeyUsedFor = KeyUsedFor.Encryption,
            ProviderTypeCode = asymmetricEncryptionProvider.EncryptionTypeCode,
            Version = systemKeyStoreProvider.GetCurrentVersion(),
            PublicKey = publickey,
            PrivateKey = new SymmetricEncryptedValue(symmetricEncryptionProvider)
            {
                Secret = encryptedPrivateKey,
            },
        };
        return asymmetricKey;
    }

    public AsymmetricKey GetAsymmetricSignatureKeyEncryptedWithSystemKey()
    {
        var systemKeyStoreProvider = _securityConfigurationProvider.GetSystemSymmetricKey();
        var asymmetricSignatureProvider = _asymmetricSignatureProviderFactory.GetDefaultProvider();
        var symmetricEncryptionProvider = _symmetricEncryptionProviderFactory.GetDefaultProvider();

        var (publickey, privateKey) = asymmetricSignatureProvider
            .GetAsymmetricKeyProvider()
            .CreateKeys();
        var encryptedPrivateKey = symmetricEncryptionProvider.Encrypt(
            privateKey,
            systemKeyStoreProvider
        );

        var asymmetricKey = new AsymmetricKey
        {
            KeyUsedFor = KeyUsedFor.Signature,
            ProviderTypeCode = asymmetricSignatureProvider.SignatureTypeCode,
            Version = systemKeyStoreProvider.GetCurrentVersion(),
            PublicKey = publickey,
            PrivateKey = new SymmetricEncryptedValue(symmetricEncryptionProvider)
            {
                Secret = encryptedPrivateKey,
            },
        };
        return asymmetricKey;
    }

    public string DecryptWithSystemKey(string encryptedValue)
    {
        if (string.IsNullOrWhiteSpace(encryptedValue))
        {
            return string.Empty;
        }

        var systemKeyStoreProvider = _securityConfigurationProvider.GetSystemSymmetricKey();

        var symmetricEncryptionProvider = _symmetricEncryptionProviderFactory.GetDefaultProvider();
        return symmetricEncryptionProvider.Decrypt(encryptedValue, systemKeyStoreProvider);
    }

    public SigningCredentials GetAsymmetricSigningCredentials(
        string providerTypeCode,
        string key,
        bool isKeyPrivate
    )
    {
        var asymmetricSignatureProvider = _asymmetricSignatureProviderFactory.GetProvider(
            providerTypeCode
        );
        return asymmetricSignatureProvider.GetSigningCredentials(key, isKeyPrivate);
    }
}
