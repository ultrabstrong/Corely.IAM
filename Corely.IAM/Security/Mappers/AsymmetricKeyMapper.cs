using Corely.IAM.Accounts.Entities;
using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Entities;
using Corely.Security.Encryption.Factories;

namespace Corely.IAM.Security.Mappers;

internal static class AsymmetricKeyMapper
{
    public static AsymmetricKeyEntity ToEntity(
        this AsymmetricKey asymmetricKey,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory
    )
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

    public static AsymmetricKey ToModel(
        this AsymmetricKeyEntity entity,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory
    )
    {
        return new AsymmetricKey
        {
            Id = 0,
            KeyUsedFor = entity.KeyUsedFor,
            ProviderTypeCode = entity.ProviderTypeCode,
            Version = entity.Version,
            PublicKey = entity.PublicKey,
            PrivateKey = entity.EncryptedPrivateKey.ToEncryptedValue(encryptionProviderFactory),
            CreatedUtc = entity.CreatedUtc,
            ModifiedUtc = entity.ModifiedUtc,
        };
    }

    public static UserAsymmetricKeyEntity ToUserEntity(
        this AsymmetricKey asymmetricKey,
        int userId,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory
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

    public static AccountAsymmetricKeyEntity ToAccountEntity(
        this AsymmetricKey asymmetricKey,
        int accountId,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory
    )
    {
        return new AccountAsymmetricKeyEntity
        {
            Id = asymmetricKey.Id,
            AccountId = accountId,
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
        this AccountAsymmetricKeyEntity entity,
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
