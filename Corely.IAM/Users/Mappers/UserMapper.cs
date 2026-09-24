using Corely.IAM.Accounts.Mappers;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Mappers;

internal static class UserMapper
{
    extension(CreateUserRequest request)
    {
        public User ToUser()
        {
            return new User { Username = request.Username, Email = request.Email };
        }
    }

    extension(User user)
    {
        public UserEntity ToEntity()
        {
            return new UserEntity
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                LockedUtc = user.LockedUtc,
                TotalSuccessfulLogins = user.TotalSuccessfulLogins,
                LastSuccessfulLoginUtc = user.LastSuccessfulLoginUtc,
                FailedLoginsSinceLastSuccess = user.FailedLoginsSinceLastSuccess,
                TotalFailedLogins = user.TotalFailedLogins,
                LastFailedLoginUtc = user.LastFailedLoginUtc,
                CreatedUtc = user.CreatedUtc,
                ModifiedUtc = user.ModifiedUtc,
                SymmetricKeys = user.SymmetricKeys?.Select(k => k.ToUserEntity(user.Id)).ToList(),
                AsymmetricKeys = user.AsymmetricKeys?.Select(k => k.ToUserEntity(user.Id)).ToList(),
            };
        }
    }

    extension(UserEntity entity)
    {
        public User ToModel()
        {
            return new User
            {
                Id = entity.Id,
                Username = entity.Username,
                Email = entity.Email,
                LockedUtc = entity.LockedUtc,
                TotalSuccessfulLogins = entity.TotalSuccessfulLogins,
                LastSuccessfulLoginUtc = entity.LastSuccessfulLoginUtc,
                FailedLoginsSinceLastSuccess = entity.FailedLoginsSinceLastSuccess,
                TotalFailedLogins = entity.TotalFailedLogins,
                LastFailedLoginUtc = entity.LastFailedLoginUtc,
                CreatedUtc = entity.CreatedUtc,
                ModifiedUtc = entity.ModifiedUtc,
            };
        }

        public UserAsymmetricKeyEntity? SignatureKey() =>
            entity.AsymmetricKeys?.FirstOrDefault(k => k.KeyUsedFor == KeyUsedFor.Signature);

        public List<Account> AccountModels() =>
            entity.Accounts?.Select(a => a.ToModel()).ToList() ?? [];
    }
}
