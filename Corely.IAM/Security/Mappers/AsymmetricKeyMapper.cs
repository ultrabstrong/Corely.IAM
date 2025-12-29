using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Entities;
using Corely.Security.Encryption.Factories;

namespace Corely.IAM.Security.Mappers;

internal static class AsymmetricKeyMapper
{
    public static AsymmetricKeyEntity ToEntity(this AsymmetricKey asymmetricKey)
    {
        return new AsymmetricKeyEntity
        {
            KeyUsedFor = asymmetricKey.KeyUsedFor,
            ProviderTypeCode = asymmetricKey.ProviderTypeCode,
            Version = asymmetricKey.Version,
            PublicKey = asymmetricKey.PublicKey,
            EncryptedPrivateKey = asymmetricKey.PrivateKey.ToEncryptedString()!,
            CreatedUtc = asymmetricKey.CreatedUtc,
            ModifiedUtc = asymmetricKey.ModifiedUtc,
        };
    }

    public static UserAsymmetricKeyEntity ToUserEntity(
        this AsymmetricKey asymmetricKey,
        Guid userId
    )
    {
        return new UserAsymmetricKeyEntity
        {
            Id = asymmetricKey.Id,
            UserId = userId,
            KeyUsedFor = asymmetricKey.KeyUsedFor,
            ProviderTypeCode = asymmetricKey.ProviderTypeCode,
            Version = asymmetricKey.Version,
            PublicKey = asymmetricKey.PublicKey,
            EncryptedPrivateKey = asymmetricKey.PrivateKey.ToEncryptedString()!,
            CreatedUtc = asymmetricKey.CreatedUtc,
            ModifiedUtc = asymmetricKey.ModifiedUtc,
        };
    }

    public static AsymmetricKey ToModel(
        this UserAsymmetricKeyEntity entity,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory
    )
    {
        return new AsymmetricKey
        {
            Id = entity.Id,
            KeyUsedFor = entity.KeyUsedFor,
            ProviderTypeCode = entity.ProviderTypeCode,
            Version = entity.Version,
            PublicKey = entity.PublicKey,
            PrivateKey = entity.EncryptedPrivateKey.ToEncryptedValue(encryptionProviderFactory),
            CreatedUtc = entity.CreatedUtc,
            ModifiedUtc = entity.ModifiedUtc,
        };
    }
}
