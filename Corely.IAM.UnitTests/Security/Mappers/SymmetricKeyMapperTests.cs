using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Users.Entities;
using Corely.Security.Encryption.Factories;
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
    public void ToModel_ShouldMapAllProperties()
    {
        var entity = new UserSymmetricKeyEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = Guid.CreateVersion7(),
            KeyUsedFor = KeyUsedFor.Encryption,
            ProviderTypeCode = "AES",
            Version = 1,
            EncryptedKey = "encrypted_key",
            CreatedUtc = DateTime.UtcNow.AddDays(-1),
            ModifiedUtc = DateTime.UtcNow,
        };

        var result = entity.ToModel(_encryptionProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.KeyUsedFor, result.KeyUsedFor);
        Assert.Equal(entity.ProviderTypeCode, result.ProviderTypeCode);
        Assert.Equal(entity.Version, result.Version);
        Assert.Equal(entity.EncryptedKey, result.Key.Secret);
        Assert.Equal(entity.CreatedUtc, result.CreatedUtc);
        Assert.Equal(entity.ModifiedUtc, result.ModifiedUtc);
    }
}
