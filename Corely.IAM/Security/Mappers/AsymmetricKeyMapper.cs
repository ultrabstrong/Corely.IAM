using Corely.IAM.Accounts.Entities;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Entities;
using Corely.Security.Encryption.Factories;

namespace Corely.IAM.Security.Mappers;

internal static class AsymmetricKeyMapper
{
    extension(AsymmetricKey asymmetricKey)
    {
        public UserAsymmetricKeyEntity ToUserEntity(Guid userId)
        {
            return new UserAsymmetricKeyEntity
            {
                Id = asymmetricKey.Id,
                UserId = userId,
                KeyUsedFor = asymmetricKey.KeyUsedFor,
                ProviderName = asymmetricKey.ProviderName,
                Version = asymmetricKey.Version,
                PublicKey = asymmetricKey.PublicKey,
                EncryptedPrivateKey = asymmetricKey.PrivateKey.ToEncryptedString()!,
                CreatedUtc = asymmetricKey.CreatedUtc,
                ModifiedUtc = asymmetricKey.ModifiedUtc,
            };
        }

        public AccountAsymmetricKeyEntity ToAccountEntity(Guid accountId)
        {
            return new AccountAsymmetricKeyEntity
            {
                Id = asymmetricKey.Id,
                AccountId = accountId,
                KeyUsedFor = asymmetricKey.KeyUsedFor,
                ProviderName = asymmetricKey.ProviderName,
                Version = asymmetricKey.Version,
                PublicKey = asymmetricKey.PublicKey,
                EncryptedPrivateKey = asymmetricKey.PrivateKey.ToEncryptedString()!,
                CreatedUtc = asymmetricKey.CreatedUtc,
                ModifiedUtc = asymmetricKey.ModifiedUtc,
            };
        }
    }

    extension(UserAsymmetricKeyEntity entity)
    {
        public AsymmetricKey ToModel(ISymmetricEncryptionProviderFactory encryptionProviderFactory)
        {
            return new AsymmetricKey
            {
                Id = entity.Id,
                KeyUsedFor = entity.KeyUsedFor,
                ProviderName = entity.ProviderName,
                Version = entity.Version,
                PublicKey = entity.PublicKey,
                PrivateKey = entity.EncryptedPrivateKey.ToEncryptedValue(encryptionProviderFactory),
                CreatedUtc = entity.CreatedUtc,
                ModifiedUtc = entity.ModifiedUtc,
            };
        }
    }

    extension(AccountAsymmetricKeyEntity entity)
    {
        public AsymmetricKey ToModel(ISymmetricEncryptionProviderFactory encryptionProviderFactory)
        {
            return new AsymmetricKey
            {
                Id = entity.Id,
                KeyUsedFor = entity.KeyUsedFor,
                ProviderName = entity.ProviderName,
                Version = entity.Version,
                PublicKey = entity.PublicKey,
                PrivateKey = entity.EncryptedPrivateKey.ToEncryptedValue(encryptionProviderFactory),
                CreatedUtc = entity.CreatedUtc,
                ModifiedUtc = entity.ModifiedUtc,
            };
        }
    }
}
