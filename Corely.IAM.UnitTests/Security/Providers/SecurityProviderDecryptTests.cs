using Corely.IAM.Security.Providers;
using Corely.Security.Encryption;
using Corely.Security.Encryption.Factories;
using Corely.Security.KeyStore;
using Corely.Security.Signature.Factories;

namespace Corely.IAM.UnitTests.Security.Providers;

/// <summary>
/// A stored value names the provider that wrote it, so it must be read back by that provider
/// rather than by whichever provider happens to be the default now. Deliberately phrased in terms
/// of "an older default" rather than naming CBC, so the test keeps its meaning the next time the
/// default moves.
/// </summary>
public class SecurityProviderDecryptTests
{
    private const string NON_DEFAULT_PROVIDER = SymmetricEncryptionConstants.AES_CODE;
    private const string CURRENT_DEFAULT_PROVIDER = SymmetricEncryptionConstants.AES_GCM_CODE;

    private readonly ISymmetricKeyStoreProvider _systemKeyStore;
    private readonly SecurityProvider _securityProvider;

    public SecurityProviderDecryptTests()
    {
        var key = new SymmetricEncryptionProviderFactory(CURRENT_DEFAULT_PROVIDER)
            .GetDefaultProvider()
            .GetSymmetricKeyProvider()
            .CreateKey();
        _systemKeyStore = new InMemorySymmetricKeyStoreProvider(key);

        var mockSecurityConfig = new Mock<ISecurityConfigurationProvider>();
        mockSecurityConfig.Setup(x => x.GetSystemSymmetricKey()).Returns(_systemKeyStore);

        _securityProvider = new SecurityProvider(
            mockSecurityConfig.Object,
            new SymmetricEncryptionProviderFactory(CURRENT_DEFAULT_PROVIDER),
            new AsymmetricEncryptionProviderFactory(
                Corely.Security.Encryption.AsymmetricEncryptionConstants.RSA_CODE
            ),
            new AsymmetricSignatureProviderFactory(
                Corely.Security.Signature.AsymmetricSignatureConstants.RSA_SHA256_CODE
            )
        );
    }

    [Fact]
    public void DecryptWithSystemKey_ReadsAValueWrittenByAnOlderDefault()
    {
        const string plaintext = "value written before the default changed";

        // Written when the default was something else - the shape every 1.x database is in.
        var encrypted = new SymmetricEncryptionProviderFactory(NON_DEFAULT_PROVIDER)
            .GetDefaultProvider()
            .Encrypt(plaintext, _systemKeyStore);

        Assert.StartsWith(NON_DEFAULT_PROVIDER, encrypted);

        var decrypted = _securityProvider.DecryptWithSystemKey(encrypted);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void DecryptWithSystemKey_StillReadsAValueWrittenByTheCurrentDefault()
    {
        const string plaintext = "value written by the current default";
        var encrypted = new SymmetricEncryptionProviderFactory(CURRENT_DEFAULT_PROVIDER)
            .GetDefaultProvider()
            .Encrypt(plaintext, _systemKeyStore);

        var decrypted = _securityProvider.DecryptWithSystemKey(encrypted);

        Assert.Equal(plaintext, decrypted);
    }
}
