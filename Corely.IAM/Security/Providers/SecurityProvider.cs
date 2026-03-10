using Corely.Common.Extensions;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Models;
using Corely.Security.KeyStore;
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
            Id = Guid.CreateVersion7(),
            KeyUsedFor = KeyUsedFor.Encryption,
            ProviderName = symmetricEncryptionProvider.ProviderName,
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
            Id = Guid.CreateVersion7(),
            KeyUsedFor = KeyUsedFor.Encryption,
            ProviderName = asymmetricEncryptionProvider.ProviderName,
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
            Id = Guid.CreateVersion7(),
            KeyUsedFor = KeyUsedFor.Signature,
            ProviderName = asymmetricSignatureProvider.ProviderName,
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
        string providerName,
        string key,
        bool isKeyPrivate
    )
    {
        var asymmetricSignatureProvider = _asymmetricSignatureProviderFactory.GetProvider(
            providerName
        );
        return asymmetricSignatureProvider.GetSigningCredentials(key, isKeyPrivate);
    }

    public IIamSymmetricEncryptionProvider BuildSymmetricEncryptionProvider(
        SymmetricKey symmetricKey
    )
    {
        var systemKeyStore = _securityConfigurationProvider.GetSystemSymmetricKey();
        var decryptedKey = symmetricKey.Key.GetDecrypted(systemKeyStore);
        var keyStore = new InMemorySymmetricKeyStoreProvider(decryptedKey);
        // Key material is algorithm-specific — must use the provider that matches
        // the algorithm used to generate the key, not the current default
        var provider = _symmetricEncryptionProviderFactory.GetProvider(symmetricKey.ProviderName);
        return new IamSymmetricEncryptionProvider(provider, keyStore, symmetricKey.ProviderName);
    }

    public IIamAsymmetricEncryptionProvider BuildAsymmetricEncryptionProvider(
        AsymmetricKey asymmetricKey
    )
    {
        var systemKeyStore = _securityConfigurationProvider.GetSystemSymmetricKey();
        var decryptedPrivateKey = asymmetricKey.PrivateKey.GetDecrypted(systemKeyStore);
        var keyStore = new InMemoryAsymmetricKeyStoreProvider(
            asymmetricKey.PublicKey,
            decryptedPrivateKey
        );
        // Key material is algorithm-specific — must use the provider that matches
        // the algorithm used to generate the key, not the current default
        var provider = _asymmetricEncryptionProviderFactory.GetProvider(asymmetricKey.ProviderName);
        return new IamAsymmetricEncryptionProvider(
            provider,
            keyStore,
            asymmetricKey.ProviderName,
            asymmetricKey.PublicKey
        );
    }

    public IIamAsymmetricSignatureProvider BuildAsymmetricSignatureProvider(
        AsymmetricKey asymmetricKey
    )
    {
        var systemKeyStore = _securityConfigurationProvider.GetSystemSymmetricKey();
        var decryptedPrivateKey = asymmetricKey.PrivateKey.GetDecrypted(systemKeyStore);
        var keyStore = new InMemoryAsymmetricKeyStoreProvider(
            asymmetricKey.PublicKey,
            decryptedPrivateKey
        );
        // Key material is algorithm-specific — must use the provider that matches
        // the algorithm used to generate the key, not the current default
        var provider = _asymmetricSignatureProviderFactory.GetProvider(asymmetricKey.ProviderName);
        return new IamAsymmetricSignatureProvider(
            provider,
            keyStore,
            asymmetricKey.ProviderName,
            asymmetricKey.PublicKey
        );
    }
}
