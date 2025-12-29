using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Users.Entities;
using Corely.Security.Encryption.Factories;
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
    public void ToModel_ShouldMapAllProperties()
    {
        var entity = new UserAsymmetricKeyEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = Guid.CreateVersion7(),
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
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.KeyUsedFor, result.KeyUsedFor);
        Assert.Equal(entity.ProviderTypeCode, result.ProviderTypeCode);
        Assert.Equal(entity.Version, result.Version);
        Assert.Equal(entity.PublicKey, result.PublicKey);
        Assert.Equal(entity.EncryptedPrivateKey, result.PrivateKey.Secret);
        Assert.Equal(entity.CreatedUtc, result.CreatedUtc);
        Assert.Equal(entity.ModifiedUtc, result.ModifiedUtc);
    }
}
