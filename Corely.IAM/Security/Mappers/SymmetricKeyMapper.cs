using Corely.IAM.Accounts.Entities;
using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Entities;
using Corely.Security.Encryption.Factories;

namespace Corely.IAM.Security.Mappers;

internal static class SymmetricKeyMapper
{
    public static SymmetricKeyEntity ToEntity(
        this SymmetricKey symmetricKey,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory
    )
    {
        return new SymmetricKeyEntity
        {
            KeyUsedFor = symmetricKey.KeyUsedFor,
            ProviderTypeCode = symmetricKey.ProviderTypeCode,
            Version = symmetricKey.Version,
            EncryptedKey = symmetricKey.Key.ToEncryptedString()!,
            CreatedUtc = symmetricKey.CreatedUtc,
            ModifiedUtc = symmetricKey.ModifiedUtc,
        };
    }

    public static SymmetricKey ToModel(
        this SymmetricKeyEntity entity,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory
    )
    {
        return new SymmetricKey
        {
            Id = 0,
            KeyUsedFor = entity.KeyUsedFor,
            ProviderTypeCode = entity.ProviderTypeCode,
            Version = entity.Version,
            Key = entity.EncryptedKey.ToEncryptedValue(encryptionProviderFactory),
            CreatedUtc = entity.CreatedUtc,
            ModifiedUtc = entity.ModifiedUtc,
        };
    }

    public static UserSymmetricKeyEntity ToUserEntity(
        this SymmetricKey symmetricKey,
        int userId,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory
    )
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

    public static AccountSymmetricKeyEntity ToAccountEntity(
        this SymmetricKey symmetricKey,
        int accountId,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory
    )
    {
        return new AccountSymmetricKeyEntity
        {
            Id = symmetricKey.Id,
            AccountId = accountId,
            KeyUsedFor = symmetricKey.KeyUsedFor,
            ProviderTypeCode = symmetricKey.ProviderTypeCode,
            Version = symmetricKey.Version,
            EncryptedKey = symmetricKey.Key.ToEncryptedString()!,
            CreatedUtc = symmetricKey.CreatedUtc,
            ModifiedUtc = symmetricKey.ModifiedUtc,
        };
    }

    public static SymmetricKey ToModel(
        this AccountSymmetricKeyEntity entity,
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
