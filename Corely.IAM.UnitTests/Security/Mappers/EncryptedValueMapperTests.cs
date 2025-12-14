using Corely.IAM.Security.Mappers;
using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Models;
using Corely.Security.Encryption.Providers;

namespace Corely.IAM.UnitTests.Security.Mappers;

public class EncryptedValueMapperTests
{
    private readonly ISymmetricEncryptionProviderFactory _encryptionProviderFactory;
    private readonly ISymmetricEncryptionProvider _encryptionProvider;

    public EncryptedValueMapperTests()
    {
        _encryptionProvider = Mock.Of<ISymmetricEncryptionProvider>();
        _encryptionProviderFactory = Mock.Of<ISymmetricEncryptionProviderFactory>(f =>
            f.GetProviderForDecrypting(It.IsAny<string>()) == _encryptionProvider
        );
    }

    [Fact]
    public void ToEncryptedString_ShouldReturnSecret_WhenSourceIsValid()
    {
        // Arrange
        var encryptedValue = new SymmetricEncryptedValue(_encryptionProvider)
        {
            Secret = "encrypted_test_data",
        };

        // Act
        var result = encryptedValue.ToEncryptedString();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("encrypted_test_data", result);
    }

    [Fact]
    public void ToEncryptedString_ShouldReturnNull_WhenSourceIsNull()
    {
        // Arrange
        ISymmetricEncryptedValue? encryptedValue = null;

        // Act
        var result = encryptedValue.ToEncryptedString();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToEncryptedValue_ShouldCreateEncryptedValue_WhenSourceIsValid()
    {
        // Arrange
        var encryptedString = "encrypted_test_secret";

        // Act
        var result = encryptedString.ToEncryptedValue(_encryptionProviderFactory);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(encryptedString, result.Secret);
    }

    [Fact]
    public void ToEncryptedString_ToEncryptedValue_RoundTrip_ShouldPreserveSecret()
    {
        // Arrange
        var originalValue = new SymmetricEncryptedValue(_encryptionProvider)
        {
            Secret = "original_encrypted_secret",
        };
        var originalSecret = originalValue.Secret;

        // Act
        var encryptedString = originalValue.ToEncryptedString();
        var resultValue = encryptedString!.ToEncryptedValue(_encryptionProviderFactory);

        // Assert
        Assert.Equal(originalSecret, resultValue.Secret);
    }

    [Theory]
    [InlineData("encrypted_password123")]
    [InlineData("encrypted_email")]
    [InlineData("encrypted_complex_data")]
    public void ToEncryptedValue_ShouldHandleVariousInputs(string encryptedString)
    {
        // Arrange & Act
        var result = encryptedString.ToEncryptedValue(_encryptionProviderFactory);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(encryptedString, result.Secret);
    }
}
