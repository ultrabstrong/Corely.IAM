using Corely.IAM.Security.Models;
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
}
