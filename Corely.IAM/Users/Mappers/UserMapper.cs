using Corely.IAM.Security.Mappers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.Security.Encryption.Factories;

namespace Corely.IAM.Users.Mappers;

internal static class UserMapper
{
    public static User ToUser(this CreateUserRequest request)
    {
        return new User { Username = request.Username, Email = request.Email };
    }

    public static UserEntity ToEntity(this User user)
    {
        return new UserEntity
        {
            Id = user.Id,
            PublicId = user.PublicId,
            Username = user.Username,
            Email = user.Email,
            Disabled = user.Disabled,
            TotalSuccessfulLogins = user.TotalSuccessfulLogins,
            LastSuccessfulLoginUtc = user.LastSuccessfulLoginUtc,
            FailedLoginsSinceLastSuccess = user.FailedLoginsSinceLastSuccess,
            TotalFailedLogins = user.TotalFailedLogins,
            LastFailedLoginUtc = user.LastFailedLoginUtc,
            CreatedUtc = user.CreatedUtc,
            ModifiedUtc = user.ModifiedUtc,
        };
    }

    public static UserEntity ToEntity(
        this User user,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory
    )
    {
        return new UserEntity
        {
            Id = user.Id,
            PublicId = user.PublicId,
            Username = user.Username,
            Email = user.Email,
            Disabled = user.Disabled,
            TotalSuccessfulLogins = user.TotalSuccessfulLogins,
            LastSuccessfulLoginUtc = user.LastSuccessfulLoginUtc,
            FailedLoginsSinceLastSuccess = user.FailedLoginsSinceLastSuccess,
            TotalFailedLogins = user.TotalFailedLogins,
            LastFailedLoginUtc = user.LastFailedLoginUtc,
            CreatedUtc = user.CreatedUtc,
            ModifiedUtc = user.ModifiedUtc,
            SymmetricKeys = user
                .SymmetricKeys?.Select(k => k.ToUserEntity(user.Id, encryptionProviderFactory))
                .ToList(),
            AsymmetricKeys = user
                .AsymmetricKeys?.Select(k => k.ToUserEntity(user.Id, encryptionProviderFactory))
                .ToList(),
        };
    }

    public static User ToModel(this UserEntity entity)
    {
        return new User
        {
            Id = entity.Id,
            PublicId = entity.PublicId,
            Username = entity.Username,
            Email = entity.Email,
            Disabled = entity.Disabled,
            TotalSuccessfulLogins = entity.TotalSuccessfulLogins,
            LastSuccessfulLoginUtc = entity.LastSuccessfulLoginUtc,
            FailedLoginsSinceLastSuccess = entity.FailedLoginsSinceLastSuccess,
            TotalFailedLogins = entity.TotalFailedLogins,
            LastFailedLoginUtc = entity.LastFailedLoginUtc,
            CreatedUtc = entity.CreatedUtc,
            ModifiedUtc = entity.ModifiedUtc,
        };
    }

    public static User ToModel(
        this UserEntity entity,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory
    )
    {
        return new User
        {
            Id = entity.Id,
            PublicId = entity.PublicId,
            Username = entity.Username,
            Email = entity.Email,
            Disabled = entity.Disabled,
            TotalSuccessfulLogins = entity.TotalSuccessfulLogins,
            LastSuccessfulLoginUtc = entity.LastSuccessfulLoginUtc,
            FailedLoginsSinceLastSuccess = entity.FailedLoginsSinceLastSuccess,
            TotalFailedLogins = entity.TotalFailedLogins,
            LastFailedLoginUtc = entity.LastFailedLoginUtc,
            CreatedUtc = entity.CreatedUtc,
            ModifiedUtc = entity.ModifiedUtc,
            SymmetricKeys = entity
                .SymmetricKeys?.Select(k => k.ToModel(encryptionProviderFactory))
                .ToList(),
            AsymmetricKeys = entity
                .AsymmetricKeys?.Select(k => k.ToModel(encryptionProviderFactory))
                .ToList(),
        };
    }
}
