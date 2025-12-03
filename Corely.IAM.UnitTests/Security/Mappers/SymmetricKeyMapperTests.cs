using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Entities;
using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Models;
using Corely.Security.Encryption.Providers;

namespace Corely.IAM.UnitTests.Security.Mappers;

public class SymmetricKeyMapperTests
{
    private readonly ISymmetricEncryptionProviderFactory _encryptionProviderFactory;
    private readonly ISymmetricEncryptionProvider _encryptionProvider;

    public SymmetricKeyMapperTests()
    {
        _encryptionProvider = Mock.Of<ISymmetricEncryptionProvider>();
        _encryptionProviderFactory = Mock.Of<ISymmetricEncryptionProviderFactory>(f =>
            f.GetProviderForDecrypting(It.IsAny<string>()) == _encryptionProvider
        );
    }

    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        var symmetricKey = new SymmetricKey
        {
            Id = 42,
            KeyUsedFor = KeyUsedFor.Encryption,
            ProviderTypeCode = "AES",
            Version = 1,
            Key = new SymmetricEncryptedValue(_encryptionProvider) { Secret = "encrypted_key" },
            CreatedUtc = DateTime.UtcNow.AddDays(-1),
            ModifiedUtc = DateTime.UtcNow,
        };

        var result = symmetricKey.ToEntity(_encryptionProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(KeyUsedFor.Encryption, result.KeyUsedFor);
        Assert.Equal("AES", result.ProviderTypeCode);
        Assert.Equal(1, result.Version);
        Assert.Equal("encrypted_key", result.EncryptedKey);
        Assert.Equal(symmetricKey.CreatedUtc, result.CreatedUtc);
        Assert.Equal(symmetricKey.ModifiedUtc, result.ModifiedUtc);
    }

    [Fact]
    public void ToModel_ShouldMapAllProperties()
    {
        var entity = new UserSymmetricKeyEntity
        {
            Id = 42,
            UserId = 123,
            KeyUsedFor = KeyUsedFor.Encryption,
            ProviderTypeCode = "AES",
            Version = 1,
            EncryptedKey = "encrypted_key",
            CreatedUtc = DateTime.UtcNow.AddDays(-1),
            ModifiedUtc = DateTime.UtcNow,
        };

        var result = entity.ToModel(_encryptionProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal(KeyUsedFor.Encryption, result.KeyUsedFor);
        Assert.Equal("AES", result.ProviderTypeCode);
        Assert.Equal(1, result.Version);
        Assert.Equal("encrypted_key", result.Key.Secret);
        Assert.Equal(entity.CreatedUtc, result.CreatedUtc);
        Assert.Equal(entity.ModifiedUtc, result.ModifiedUtc);
    }

    [Fact]
    public void ToUserEntity_ShouldMapAllProperties()
    {
        var symmetricKey = new SymmetricKey
        {
            Id = 42,
            KeyUsedFor = KeyUsedFor.Encryption,
            ProviderTypeCode = "AES",
            Version = 1,
            Key = new SymmetricEncryptedValue(_encryptionProvider) { Secret = "encrypted_key" },
            CreatedUtc = DateTime.UtcNow.AddDays(-1),
            ModifiedUtc = DateTime.UtcNow,
        };

        var result = symmetricKey.ToUserEntity(123, _encryptionProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal(123, result.UserId);
        Assert.Equal(KeyUsedFor.Encryption, result.KeyUsedFor);
        Assert.Equal("AES", result.ProviderTypeCode);
        Assert.Equal(1, result.Version);
        Assert.Equal("encrypted_key", result.EncryptedKey);
    }

    [Fact]
    public void ToModel_ToUserEntity_RoundTrip_ShouldPreserveData()
    {
        var originalKey = new SymmetricKey
        {
            Id = 99,
            KeyUsedFor = KeyUsedFor.Encryption,
            ProviderTypeCode = "AES",
            Version = 2,
            Key = new SymmetricEncryptedValue(_encryptionProvider) { Secret = "test_key" },
            CreatedUtc = DateTime.UtcNow.AddDays(-5),
            ModifiedUtc = DateTime.UtcNow,
        };

        var entity = originalKey.ToUserEntity(456, _encryptionProviderFactory);
        var resultKey = entity.ToModel(_encryptionProviderFactory);

        Assert.Equal(originalKey.Id, resultKey.Id);
        Assert.Equal(originalKey.KeyUsedFor, resultKey.KeyUsedFor);
        Assert.Equal(originalKey.ProviderTypeCode, resultKey.ProviderTypeCode);
        Assert.Equal(originalKey.Version, resultKey.Version);
        Assert.Equal(originalKey.Key.Secret, resultKey.Key.Secret);
        Assert.Equal(originalKey.CreatedUtc, resultKey.CreatedUtc);
        Assert.Equal(originalKey.ModifiedUtc, resultKey.ModifiedUtc);
    }
}
