using System.Security.Cryptography;
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
        string encryptedKey;
        try
        {
            encryptedKey = symmetricEncryptionProvider.Encrypt(
                Convert.ToBase64String(decryptedKey),
                systemKeyStoreProvider
            );
        }
        finally
        {
            CryptographicOperations.ZeroMemory(decryptedKey);
        }

        var symmetricKey = new SymmetricKey
        {
            Id = Guid.CreateVersion7(),
            KeyUsedFor = KeyUsedFor.Encryption,
            ProviderName = symmetricEncryptionProvider.ProviderName,
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
        string encryptedPrivateKey;
        try
        {
            encryptedPrivateKey = symmetricEncryptionProvider.Encrypt(
                Convert.ToBase64String(privateKey),
                systemKeyStoreProvider
            );
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }

        var asymmetricKey = new AsymmetricKey
        {
            Id = Guid.CreateVersion7(),
            KeyUsedFor = KeyUsedFor.Encryption,
            ProviderName = asymmetricEncryptionProvider.ProviderName,
            PublicKey = Convert.ToBase64String(publickey),
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
        string encryptedPrivateKey;
        try
        {
            encryptedPrivateKey = symmetricEncryptionProvider.Encrypt(
                Convert.ToBase64String(privateKey),
                systemKeyStoreProvider
            );
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }

        var asymmetricKey = new AsymmetricKey
        {
            Id = Guid.CreateVersion7(),
            KeyUsedFor = KeyUsedFor.Signature,
            ProviderName = asymmetricSignatureProvider.ProviderName,
            PublicKey = Convert.ToBase64String(publickey),
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

        var symmetricEncryptionProvider =
            _symmetricEncryptionProviderFactory.GetProviderForDecrypting(encryptedValue);
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
        return asymmetricSignatureProvider.GetSigningCredentials(
            Convert.FromBase64String(key),
            isKeyPrivate
        );
    }

    public IIamSymmetricEncryptionProvider BuildSymmetricEncryptionProvider(
        IReadOnlyList<SymmetricKey> versions
    )
    {
        EnsureVersionsStartAtOne(versions.Select(k => k.Version));
        var systemKeyStore = _securityConfigurationProvider.GetSystemSymmetricKey();
        var keyStore = new InMemorySymmetricKeyStoreProvider(
            versions[0].Key.GetDecrypted(systemKeyStore)
        );
        foreach (var key in versions.Skip(1))
        {
            keyStore.Add(key.Key.GetDecrypted(systemKeyStore));
        }

        var current = versions[^1];
        var provider = _symmetricEncryptionProviderFactory.GetProvider(current.ProviderName);
        return new IamSymmetricEncryptionProvider(
            provider,
            keyStore,
            current.ProviderName,
            _symmetricEncryptionProviderFactory,
            current.Version
        );
    }

    public IIamAsymmetricEncryptionProvider BuildAsymmetricEncryptionProvider(
        IReadOnlyList<AsymmetricKey> versions
    )
    {
        EnsureVersionsStartAtOne(versions.Select(k => k.Version));
        var systemKeyStore = _securityConfigurationProvider.GetSystemSymmetricKey();
        var keyStore = new InMemoryAsymmetricKeyStoreProvider(
            versions[0].PublicKey,
            versions[0].PrivateKey.GetDecrypted(systemKeyStore)
        );
        foreach (var key in versions.Skip(1))
        {
            keyStore.Add(key.PublicKey, key.PrivateKey.GetDecrypted(systemKeyStore));
        }

        var current = versions[^1];
        var provider = _asymmetricEncryptionProviderFactory.GetProvider(current.ProviderName);
        return new IamAsymmetricEncryptionProvider(
            provider,
            keyStore,
            current.ProviderName,
            current.PublicKey,
            _asymmetricEncryptionProviderFactory,
            current.Version
        );
    }

    public IIamAsymmetricSignatureProvider BuildAsymmetricSignatureProvider(
        IReadOnlyList<AsymmetricKey> versions
    )
    {
        EnsureVersionsStartAtOne(versions.Select(k => k.Version));
        var systemKeyStore = _securityConfigurationProvider.GetSystemSymmetricKey();
        var current = versions[^1];
        var keyStore = new InMemoryAsymmetricKeyStoreProvider(
            current.PublicKey,
            current.PrivateKey.GetDecrypted(systemKeyStore)
        );
        var provider = _asymmetricSignatureProviderFactory.GetProvider(current.ProviderName);
        var previousVersions = versions
            .Take(versions.Count - 1)
            .Select(k =>
                (
                    _asymmetricSignatureProviderFactory.GetProvider(k.ProviderName),
                    (IAsymmetricKeyStoreProvider)
                        new InMemoryAsymmetricKeyStoreProvider(k.PublicKey, string.Empty)
                )
            )
            .ToList();
        return new IamAsymmetricSignatureProvider(
            provider,
            keyStore,
            current.ProviderName,
            current.PublicKey,
            previousVersions,
            current.Version
        );
    }

    private static void EnsureVersionsStartAtOne(IEnumerable<int> versions)
    {
        var expected = 1;
        foreach (var version in versions)
        {
            if (version != expected++)
            {
                throw new InvalidOperationException(
                    $"Key versions must run from 1 without gaps; found {version} where {expected - 1} was expected"
                );
            }
        }

        if (expected == 1)
        {
            throw new ArgumentException("At least one key version is required");
        }
    }
}
