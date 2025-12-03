using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Entities;
using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Models;
using Corely.Security.Encryption.Providers;

namespace Corely.IAM.UnitTests.Security.Mappers;

public class AsymmetricKeyMapperTests
{
    private readonly ISymmetricEncryptionProviderFactory _encryptionProviderFactory;
    private readonly ISymmetricEncryptionProvider _encryptionProvider;

    public AsymmetricKeyMapperTests()
    {
        _encryptionProvider = Mock.Of<ISymmetricEncryptionProvider>();
        _encryptionProviderFactory = Mock.Of<ISymmetricEncryptionProviderFactory>(f =>
            f.GetProviderForDecrypting(It.IsAny<string>()) == _encryptionProvider
        );
    }

    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        var asymmetricKey = new AsymmetricKey
        {
            Id = 42,
            KeyUsedFor = KeyUsedFor.Signature,
            ProviderTypeCode = "RSA",
            Version = 1,
            PublicKey = "public_key_data",
            PrivateKey = new SymmetricEncryptedValue(_encryptionProvider)
            {
                Secret = "encrypted_private_key",
            },
            CreatedUtc = DateTime.UtcNow.AddDays(-1),
            ModifiedUtc = DateTime.UtcNow,
        };

        var result = asymmetricKey.ToEntity(_encryptionProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(KeyUsedFor.Signature, result.KeyUsedFor);
        Assert.Equal("RSA", result.ProviderTypeCode);
        Assert.Equal(1, result.Version);
        Assert.Equal("public_key_data", result.PublicKey);
        Assert.Equal("encrypted_private_key", result.EncryptedPrivateKey);
        Assert.Equal(asymmetricKey.CreatedUtc, result.CreatedUtc);
        Assert.Equal(asymmetricKey.ModifiedUtc, result.ModifiedUtc);
    }

    [Fact]
    public void ToModel_ShouldMapAllProperties()
    {
        var entity = new UserAsymmetricKeyEntity
        {
            Id = 42,
            UserId = 123,
            KeyUsedFor = KeyUsedFor.Signature,
            ProviderTypeCode = "RSA",
            Version = 1,
            PublicKey = "public_key_data",
            EncryptedPrivateKey = "encrypted_private_key",
            CreatedUtc = DateTime.UtcNow.AddDays(-1),
            ModifiedUtc = DateTime.UtcNow,
        };

        var result = entity.ToModel(_encryptionProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal(KeyUsedFor.Signature, result.KeyUsedFor);
        Assert.Equal("RSA", result.ProviderTypeCode);
        Assert.Equal(1, result.Version);
        Assert.Equal("public_key_data", result.PublicKey);
        Assert.Equal("encrypted_private_key", result.PrivateKey.Secret);
        Assert.Equal(entity.CreatedUtc, result.CreatedUtc);
        Assert.Equal(entity.ModifiedUtc, result.ModifiedUtc);
    }

    [Fact]
    public void ToUserEntity_ShouldMapAllProperties()
    {
        var asymmetricKey = new AsymmetricKey
        {
            Id = 42,
            KeyUsedFor = KeyUsedFor.Signature,
            ProviderTypeCode = "RSA",
            Version = 1,
            PublicKey = "public_key_data",
            PrivateKey = new SymmetricEncryptedValue(_encryptionProvider)
            {
                Secret = "encrypted_private_key",
            },
            CreatedUtc = DateTime.UtcNow.AddDays(-1),
            ModifiedUtc = DateTime.UtcNow,
        };

        var result = asymmetricKey.ToUserEntity(123, _encryptionProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal(123, result.UserId);
        Assert.Equal(KeyUsedFor.Signature, result.KeyUsedFor);
        Assert.Equal("RSA", result.ProviderTypeCode);
        Assert.Equal(1, result.Version);
        Assert.Equal("public_key_data", result.PublicKey);
        Assert.Equal("encrypted_private_key", result.EncryptedPrivateKey);
    }

    [Fact]
    public void ToModel_ToUserEntity_RoundTrip_ShouldPreserveData()
    {
        var originalKey = new AsymmetricKey
        {
            Id = 99,
            KeyUsedFor = KeyUsedFor.Signature,
            ProviderTypeCode = "RSA",
            Version = 2,
            PublicKey = "test_public_key",
            PrivateKey = new SymmetricEncryptedValue(_encryptionProvider)
            {
                Secret = "test_private_key",
            },
            CreatedUtc = DateTime.UtcNow.AddDays(-5),
            ModifiedUtc = DateTime.UtcNow,
        };

        var entity = originalKey.ToUserEntity(456, _encryptionProviderFactory);
        var resultKey = entity.ToModel(_encryptionProviderFactory);

        Assert.Equal(originalKey.Id, resultKey.Id);
        Assert.Equal(originalKey.KeyUsedFor, resultKey.KeyUsedFor);
        Assert.Equal(originalKey.ProviderTypeCode, resultKey.ProviderTypeCode);
        Assert.Equal(originalKey.Version, resultKey.Version);
        Assert.Equal(originalKey.PublicKey, resultKey.PublicKey);
        Assert.Equal(originalKey.PrivateKey.Secret, resultKey.PrivateKey.Secret);
        Assert.Equal(originalKey.CreatedUtc, resultKey.CreatedUtc);
        Assert.Equal(originalKey.ModifiedUtc, resultKey.ModifiedUtc);
    }
}
