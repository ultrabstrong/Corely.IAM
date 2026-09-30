using Corely.IAM.Security.Models;
using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Providers;
using Corely.Security.KeyStore;

namespace Corely.IAM.UnitTests.Security.Models;

public class IamSymmetricEncryptionProviderTests
{
    private readonly Mock<ISymmetricEncryptionProvider> _mockProvider = new();
    private readonly Mock<ISymmetricKeyStoreProvider> _mockKeyStore = new();
    private readonly IamSymmetricEncryptionProvider _iamProvider;

    public IamSymmetricEncryptionProviderTests()
    {
        _iamProvider = new IamSymmetricEncryptionProvider(
            _mockProvider.Object,
            _mockKeyStore.Object,
            "AES"
        );
    }

    [Fact]
    public void Encrypt_DelegatesToProviderWithKeyStore()
    {
        var plaintext = "test plaintext";
        var expectedCiphertext = "00:0:encrypted";
        _mockProvider
            .Setup(x => x.Encrypt(plaintext, _mockKeyStore.Object))
            .Returns(expectedCiphertext);

        var result = _iamProvider.Encrypt(plaintext);

        Assert.Equal(expectedCiphertext, result);
        _mockProvider.Verify(x => x.Encrypt(plaintext, _mockKeyStore.Object), Times.Once);
    }

    [Fact]
    public void Decrypt_DelegatesToProviderWithKeyStore()
    {
        var ciphertext = "00:0:encrypted";
        var expectedPlaintext = "test plaintext";
        _mockProvider
            .Setup(x => x.Decrypt(ciphertext, _mockKeyStore.Object))
            .Returns(expectedPlaintext);

        var result = _iamProvider.Decrypt(ciphertext);

        Assert.Equal(expectedPlaintext, result);
        _mockProvider.Verify(x => x.Decrypt(ciphertext, _mockKeyStore.Object), Times.Once);
    }

    [Fact]
    public void ReEncrypt_DelegatesToProviderWithKeyStore()
    {
        var ciphertext = "00:0:encrypted";
        var expectedReEncrypted = "00:0:reencrypted";
        _mockProvider
            .Setup(x => x.ReEncrypt(ciphertext, _mockKeyStore.Object))
            .Returns(expectedReEncrypted);

        var result = _iamProvider.ReEncrypt(ciphertext);

        Assert.Equal(expectedReEncrypted, result);
        _mockProvider.Verify(x => x.ReEncrypt(ciphertext, _mockKeyStore.Object), Times.Once);
    }

    [Fact]
    public void Decrypt_UsesTheCiphertextsProvider_ForAnotherAlgorithm()
    {
        var older = new Mock<ISymmetricEncryptionProvider>();
        var factory = new Mock<ISymmetricEncryptionProviderFactory>();
        factory.Setup(x => x.GetProviderForDecrypting("OLD:1:data")).Returns(older.Object);
        older.Setup(x => x.Decrypt("OLD:1:data", _mockKeyStore.Object)).Returns("plain");
        var iamProvider = new IamSymmetricEncryptionProvider(
            _mockProvider.Object,
            _mockKeyStore.Object,
            "AES",
            factory.Object
        );

        Assert.Equal("plain", iamProvider.Decrypt("OLD:1:data"));
    }

    [Fact]
    public void ReEncrypt_EncryptsWithCurrentProvider_ForAnotherAlgorithm()
    {
        var older = new Mock<ISymmetricEncryptionProvider>();
        older.Setup(x => x.ProviderName).Returns("OLD");
        older.Setup(x => x.Decrypt("OLD:1:data", _mockKeyStore.Object)).Returns("plain");
        _mockProvider.Setup(x => x.ProviderName).Returns("AES");
        _mockProvider.Setup(x => x.Encrypt("plain", _mockKeyStore.Object)).Returns("AES:2:data");
        var factory = new Mock<ISymmetricEncryptionProviderFactory>();
        factory.Setup(x => x.GetProviderForDecrypting("OLD:1:data")).Returns(older.Object);
        var iamProvider = new IamSymmetricEncryptionProvider(
            _mockProvider.Object,
            _mockKeyStore.Object,
            "AES",
            factory.Object
        );

        Assert.Equal("AES:2:data", iamProvider.ReEncrypt("OLD:1:data"));
        _mockProvider.Verify(
            x => x.ReEncrypt(It.IsAny<string>(), It.IsAny<ISymmetricKeyStoreProvider>()),
            Times.Never
        );
    }
}
