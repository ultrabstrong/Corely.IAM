using Corely.IAM.Security.Models;
using Corely.Security.KeyStore;
using Corely.Security.Signature.Providers;

namespace Corely.IAM.UnitTests.Security.Models;

public class IamAsymmetricSignatureProviderTests
{
    private readonly Mock<IAsymmetricSignatureProvider> _mockProvider = new();
    private readonly Mock<IAsymmetricKeyStoreProvider> _mockKeyStore = new();
    private readonly IamAsymmetricSignatureProvider _iamProvider;

    public IamAsymmetricSignatureProviderTests()
    {
        _iamProvider = new IamAsymmetricSignatureProvider(
            _mockProvider.Object,
            _mockKeyStore.Object,
            "ECDSA_SHA256",
            "test-public-key"
        );
    }

    [Fact]
    public void Sign_DelegatesToProviderWithKeyStore()
    {
        var payload = "test payload";
        var expectedSignature = "signature-bytes";
        _mockProvider.Setup(x => x.Sign(payload, _mockKeyStore.Object)).Returns(expectedSignature);

        var result = _iamProvider.Sign(payload);

        Assert.Equal(expectedSignature, result);
        _mockProvider.Verify(x => x.Sign(payload, _mockKeyStore.Object), Times.Once);
    }

    [Fact]
    public void Verify_DelegatesToProviderWithKeyStore()
    {
        var payload = "test payload";
        var signature = "signature-bytes";
        _mockProvider.Setup(x => x.Verify(payload, signature, _mockKeyStore.Object)).Returns(true);

        var result = _iamProvider.Verify(payload, signature);

        Assert.True(result);
        _mockProvider.Verify(x => x.Verify(payload, signature, _mockKeyStore.Object), Times.Once);
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenSignatureInvalid()
    {
        var payload = "test payload";
        var signature = "bad-signature";
        _mockProvider.Setup(x => x.Verify(payload, signature, _mockKeyStore.Object)).Returns(false);

        var result = _iamProvider.Verify(payload, signature);

        Assert.False(result);
    }
}
