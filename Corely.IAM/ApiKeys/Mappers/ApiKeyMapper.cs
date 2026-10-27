using Corely.IAM.ApiKeys.Entities;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Groups.Mappers;
using Corely.IAM.Models;
using Corely.IAM.Roles.Mappers;

namespace Corely.IAM.ApiKeys.Mappers;

internal static class ApiKeyMapper
{
    extension(CreateApiKeyRequest request)
    {
        public ApiKey ToApiKey() =>
            new()
            {
                Name = request.Name?.Trim()!,
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
                AccountId = request.OwnerAccountId,
            };
    }

    extension(ApiKey apiKey)
    {
        public ApiKeyEntity ToEntity() =>
            new()
            {
                Id = apiKey.Id,
                AccountId = apiKey.AccountId,
                Name = apiKey.Name,
                Description = apiKey.Description,
            };
    }

    extension(ApiKeyEntity entity)
    {
        public ChildRef ToChildRef() => new(entity.Id, entity.Name);

        public ApiKey ToModel() =>
            new()
            {
                Id = entity.Id,
                AccountId = entity.AccountId,
                Name = entity.Name,
                Description = entity.Description,
                LockedUtc = entity.LockedUtc,
                TotalSuccessfulLogins = entity.TotalSuccessfulLogins,
                LastSuccessfulLoginUtc = entity.LastSuccessfulLoginUtc,
                FailedLoginsSinceLastSuccess = entity.FailedLoginsSinceLastSuccess,
                TotalFailedLogins = entity.TotalFailedLogins,
                LastFailedLoginUtc = entity.LastFailedLoginUtc,
                CreatedUtc = entity.CreatedUtc,
                ModifiedUtc = entity.ModifiedUtc,
                Secrets = [.. (entity.Secrets ?? []).OrderBy(s => s.Slot).Select(s => s.ToInfo())],
                Groups = entity.Groups?.Select(g => g.ToChildRef()).ToList(),
                Roles = entity.Roles?.Select(r => r.ToChildRef()).ToList(),
            };
    }

    extension(ApiKeySecretEntity secret)
    {
        public ApiKeySecretInfo ToInfo() =>
            new(
                secret.Slot,
                secret.Prefix,
                secret.CreatedUtc,
                secret.ExpiresUtc,
                secret.LastUsedUtc
            );
    }
}
