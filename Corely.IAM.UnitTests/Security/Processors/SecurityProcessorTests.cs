using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.UnitTests.ClassData;
using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Providers;
using Corely.Security.Signature.Factories;
using Corely.Security.Signature.Providers;

namespace Corely.IAM.UnitTests.Security.Processors;

public class SecurityProcessorTests
{
    private readonly ISecurityConfigurationProvider _securityConfigurationProvider;
    private readonly ISymmetricEncryptionProvider _symmetricEncryptionProvider;
    private readonly IAsymmetricEncryptionProvider _asymmetricEncryptionProvider;
    private readonly IAsymmetricSignatureProvider _asymmetricSignatureProvider;
    private readonly SecurityProvider _securityProcessor;

    public SecurityProcessorTests()
    {
        var serviceFactory = new ServiceFactory();

        _securityConfigurationProvider =
            serviceFactory.GetRequiredService<ISecurityConfigurationProvider>();

        var symmetricEncryptionProviderFactory =
            serviceFactory.GetRequiredService<ISymmetricEncryptionProviderFactory>();
        _symmetricEncryptionProvider = symmetricEncryptionProviderFactory.GetDefaultProvider();

        var asymmetricEncryptionProviderFactory =
            serviceFactory.GetRequiredService<IAsymmetricEncryptionProviderFactory>();
        _asymmetricEncryptionProvider = asymmetricEncryptionProviderFactory.GetDefaultProvider();

        var asymmetricSignatureProviderFactory =
            serviceFactory.GetRequiredService<IAsymmetricSignatureProviderFactory>();
        _asymmetricSignatureProvider = asymmetricSignatureProviderFactory.GetDefaultProvider();

        _securityProcessor = new(
            _securityConfigurationProvider,
            symmetricEncryptionProviderFactory,
            asymmetricEncryptionProviderFactory,
            asymmetricSignatureProviderFactory
        );
    }

    [Fact]
    public void GetSymmetricEncryptionKeyEncryptedWithSystemKey_ReturnsSymmetricKey()
    {
        var result = _securityProcessor.GetSymmetricEncryptionKeyEncryptedWithSystemKey();

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotNull(result.Key);
        Assert.True(result.Version > -1);
        Assert.Equal(KeyUsedFor.Encryption, result.KeyUsedFor);
        Assert.Equal(_symmetricEncryptionProvider.EncryptionTypeCode, result.ProviderTypeCode);

        var decryptedKey = _securityProcessor.DecryptWithSystemKey(result.Key.Secret);

        Assert.True(
            _symmetricEncryptionProvider.GetSymmetricKeyProvider().IsKeyValid(decryptedKey)
        );
    }

    [Fact]
    public void GetAsymmetricEncryptionKeyEncryptedWithSystemKey_ReturnsAsymmetricKey()
    {
        var result = _securityProcessor.GetAsymmetricEncryptionKeyEncryptedWithSystemKey();

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotNull(result);
        Assert.NotNull(result.PublicKey);
        Assert.NotNull(result.PrivateKey);
        Assert.True(result.Version > -1);
        Assert.Equal(KeyUsedFor.Encryption, result.KeyUsedFor);
        Assert.Equal(_asymmetricEncryptionProvider.EncryptionTypeCode, result.ProviderTypeCode);

        var decryptedPrivateKey = _securityProcessor.DecryptWithSystemKey(result.PrivateKey.Secret);

        Assert.True(
            _asymmetricEncryptionProvider
                .GetAsymmetricKeyProvider()
                .IsKeyValid(result.PublicKey, decryptedPrivateKey)
        );
    }

    [Fact]
    public void GetAsymmetricSignatureKeyEncryptedWithSystemKey_ReturnsAsymmetricKey()
    {
        var result = _securityProcessor.GetAsymmetricSignatureKeyEncryptedWithSystemKey();

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotNull(result);
        Assert.NotNull(result.PublicKey);
        Assert.NotNull(result.PrivateKey);
        Assert.True(result.Version > -1);
        Assert.Equal(KeyUsedFor.Signature, result.KeyUsedFor);
        Assert.Equal(_asymmetricSignatureProvider.SignatureTypeCode, result.ProviderTypeCode);

        var decryptedPrivateKey = _securityProcessor.DecryptWithSystemKey(result.PrivateKey.Secret);

        Assert.True(
            _asymmetricSignatureProvider
                .GetAsymmetricKeyProvider()
                .IsKeyValid(result.PublicKey, decryptedPrivateKey)
        );
    }

    [Fact]
    public void DecryptWithSystemKey_ReturnsDecryptedValue()
    {
        var symmetricKey = _securityProcessor.GetSymmetricEncryptionKeyEncryptedWithSystemKey();
        var expectedDecryptedValue = symmetricKey.Key.GetDecrypted(
            _securityConfigurationProvider.GetSystemSymmetricKey()
        );

        var decryptedValue = _securityProcessor.DecryptWithSystemKey(symmetricKey.Key.Secret);

        Assert.Equal(expectedDecryptedValue, decryptedValue);
    }

    [Theory, ClassData(typeof(NullEmptyAndWhitespace))]
    public void DecryptWithSystemKey_ReturnsEmptyString_WithEmptyInput(string encryptedValue)
    {
        var decryptedValue = _securityProcessor.DecryptWithSystemKey(encryptedValue);

        Assert.Equal(string.Empty, decryptedValue);
    }

    [Fact]
    public void GetAsymmetricSigningCredentials_ReturnsSigningCredentials_WithPrivateKey()
    {
        var asymmetricKey = _securityProcessor.GetAsymmetricSignatureKeyEncryptedWithSystemKey();
        var privateKey = _securityProcessor.DecryptWithSystemKey(asymmetricKey.PrivateKey.Secret);

        var credentials = _securityProcessor.GetAsymmetricSigningCredentials(
            asymmetricKey.ProviderTypeCode,
            privateKey,
            true
        );

        Assert.NotNull(credentials);
    }

    [Fact]
    public void GetAsymmetricSigningCredentials_ReturnsSigningCredentials_WithPublicKey()
    {
        var asymmetricKey = _securityProcessor.GetAsymmetricSignatureKeyEncryptedWithSystemKey();
        var publicKey = asymmetricKey.PublicKey;

        var credentials = _securityProcessor.GetAsymmetricSigningCredentials(
            asymmetricKey.ProviderTypeCode,
            publicKey,
            false
        );

        Assert.NotNull(credentials);
    }

    [Fact]
    public void BuildSymmetricEncryptionProvider_ReturnsProvider_ThatCanEncryptAndDecrypt()
    {
        var symmetricKey = _securityProcessor.GetSymmetricEncryptionKeyEncryptedWithSystemKey();

        var provider = _securityProcessor.BuildSymmetricEncryptionProvider(symmetricKey);

        Assert.NotNull(provider);
        var plaintext = "Hello, World!";
        var encrypted = provider.Encrypt(plaintext);
        Assert.NotEqual(plaintext, encrypted);
        var decrypted = provider.Decrypt(encrypted);
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void BuildSymmetricEncryptionProvider_ReturnsProvider_ThatCanReEncrypt()
    {
        var symmetricKey = _securityProcessor.GetSymmetricEncryptionKeyEncryptedWithSystemKey();
        var provider = _securityProcessor.BuildSymmetricEncryptionProvider(symmetricKey);
        var plaintext = "ReEncrypt test";
        var encrypted = provider.Encrypt(plaintext);

        var reEncrypted = provider.ReEncrypt(encrypted);

        Assert.NotEqual(encrypted, reEncrypted);
        var decrypted = provider.Decrypt(reEncrypted);
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void BuildAsymmetricEncryptionProvider_ReturnsProvider_ThatCanEncryptAndDecrypt()
    {
        var asymmetricKey = _securityProcessor.GetAsymmetricEncryptionKeyEncryptedWithSystemKey();

        var provider = _securityProcessor.BuildAsymmetricEncryptionProvider(asymmetricKey);

        Assert.NotNull(provider);
        var plaintext = "Hello, World!";
        var encrypted = provider.Encrypt(plaintext);
        Assert.NotEqual(plaintext, encrypted);
        var decrypted = provider.Decrypt(encrypted);
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void BuildAsymmetricSignatureProvider_ReturnsProvider_ThatCanSignAndVerify()
    {
        var asymmetricKey = _securityProcessor.GetAsymmetricSignatureKeyEncryptedWithSystemKey();

        var provider = _securityProcessor.BuildAsymmetricSignatureProvider(asymmetricKey);

        Assert.NotNull(provider);
        var payload = "Hello, World!";
        var signature = provider.Sign(payload);
        Assert.NotNull(signature);
        Assert.True(provider.Verify(payload, signature));
    }

    [Fact]
    public void BuildAsymmetricSignatureProvider_ReturnsFalse_ForTamperedPayload()
    {
        var asymmetricKey = _securityProcessor.GetAsymmetricSignatureKeyEncryptedWithSystemKey();
        var provider = _securityProcessor.BuildAsymmetricSignatureProvider(asymmetricKey);
        var payload = "Original payload";
        var signature = provider.Sign(payload);

        Assert.False(provider.Verify("Tampered payload", signature));
    }
}
