using Corely.IAM.FederatedIdentities.Entities;
using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.IdentityProviders.Models;

namespace Corely.IAM.FederatedIdentities.Mappers;

internal static class FederatedIdentityMapper
{
    extension(FederatedIdentityEntity entity)
    {
        public LinkedIdentity ToModel() =>
            new(entity.ProviderType, entity.Email, entity.CreatedUtc);
    }

    extension(ExternalIdentity identity)
    {
        public FederatedIdentityEntity ToEntity(Guid userId) =>
            new()
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                ProviderType = identity.Type,
                Issuer = identity.Issuer,
                Subject = identity.Subject,
                Email = identity.Email,
                EmailVerified = identity.EmailVerified,
            };
    }
}
