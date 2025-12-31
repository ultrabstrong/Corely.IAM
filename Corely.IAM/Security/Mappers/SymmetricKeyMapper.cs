using Corely.IAM.Security.Models;
using Corely.IAM.Users.Entities;
using Corely.Security.Encryption.Factories;

namespace Corely.IAM.Security.Mappers;

internal static class SymmetricKeyMapper
{
    public static UserSymmetricKeyEntity ToUserEntity(this SymmetricKey symmetricKey, Guid userId)
    {
        return new UserSymmetricKeyEntity
        {
            Id = symmetricKey.Id,
            UserId = userId,
            KeyUsedFor = symmetricKey.KeyUsedFor,
            ProviderTypeCode = symmetricKey.ProviderTypeCode,
            Version = symmetricKey.Version,
            EncryptedKey = symmetricKey.Key.ToEncryptedString()!,
            CreatedUtc = symmetricKey.CreatedUtc,
            ModifiedUtc = symmetricKey.ModifiedUtc,
        };
    }

    public static SymmetricKey ToModel(
        this UserSymmetricKeyEntity entity,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory
    )
    {
        return new SymmetricKey
        {
            Id = entity.Id,
            KeyUsedFor = entity.KeyUsedFor,
            ProviderTypeCode = entity.ProviderTypeCode,
            Version = entity.Version,
            Key = entity.EncryptedKey.ToEncryptedValue(encryptionProviderFactory),
            CreatedUtc = entity.CreatedUtc,
            ModifiedUtc = entity.ModifiedUtc,
        };
    }
}
