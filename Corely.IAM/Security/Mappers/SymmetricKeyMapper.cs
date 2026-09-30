using Corely.IAM.Accounts.Entities;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Entities;
using Corely.Security.Encryption.Factories;

namespace Corely.IAM.Security.Mappers;

internal static class SymmetricKeyMapper
{
    extension(SymmetricKey symmetricKey)
    {
        public UserSymmetricKeyEntity ToUserEntity(Guid userId)
        {
            return new UserSymmetricKeyEntity
            {
                Id = symmetricKey.Id,
                UserId = userId,
                KeyUsedFor = symmetricKey.KeyUsedFor,
                ProviderName = symmetricKey.ProviderName,
                Version = symmetricKey.Version,
                EncryptedKey = symmetricKey.Key.ToEncryptedString()!,
                CreatedUtc = symmetricKey.CreatedUtc,
                ModifiedUtc = symmetricKey.ModifiedUtc,
            };
        }

        public AccountSymmetricKeyEntity ToAccountEntity(Guid accountId)
        {
            return new AccountSymmetricKeyEntity
            {
                Id = symmetricKey.Id,
                AccountId = accountId,
                KeyUsedFor = symmetricKey.KeyUsedFor,
                ProviderName = symmetricKey.ProviderName,
                Version = symmetricKey.Version,
                EncryptedKey = symmetricKey.Key.ToEncryptedString()!,
                CreatedUtc = symmetricKey.CreatedUtc,
                ModifiedUtc = symmetricKey.ModifiedUtc,
            };
        }
    }

    extension(UserSymmetricKeyEntity entity)
    {
        public SymmetricKey ToModel(ISymmetricEncryptionProviderFactory encryptionProviderFactory)
        {
            return new SymmetricKey
            {
                Id = entity.Id,
                KeyUsedFor = entity.KeyUsedFor,
                ProviderName = entity.ProviderName,
                Version = entity.Version,
                Key = entity.EncryptedKey.ToEncryptedValue(encryptionProviderFactory),
                CreatedUtc = entity.CreatedUtc,
                ModifiedUtc = entity.ModifiedUtc,
            };
        }
    }

    extension(AccountSymmetricKeyEntity entity)
    {
        public SymmetricKey ToModel(ISymmetricEncryptionProviderFactory encryptionProviderFactory)
        {
            return new SymmetricKey
            {
                Id = entity.Id,
                KeyUsedFor = entity.KeyUsedFor,
                ProviderName = entity.ProviderName,
                Version = entity.Version,
                Key = entity.EncryptedKey.ToEncryptedValue(encryptionProviderFactory),
                CreatedUtc = entity.CreatedUtc,
                ModifiedUtc = entity.ModifiedUtc,
            };
        }
    }
}
