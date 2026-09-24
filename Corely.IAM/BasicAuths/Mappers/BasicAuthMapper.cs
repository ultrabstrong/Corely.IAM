using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.Security.Mappers;
using Corely.Security.Hashing.Factories;

namespace Corely.IAM.BasicAuths.Mappers;

internal static class BasicAuthMapper
{
    extension(CreateBasicAuthRequest request)
    {
        public BasicAuth ToBasicAuth(IHashProviderFactory hashProviderFactory)
        {
            return new BasicAuth
            {
                UserId = request.UserId,
                Password = request.Password.ToHashedValueFromPlainText(hashProviderFactory),
            };
        }
    }

    extension(UpdateBasicAuthRequest request)
    {
        public BasicAuth ToBasicAuth(IHashProviderFactory hashProviderFactory)
        {
            return new BasicAuth
            {
                UserId = request.UserId,
                Password = request.Password.ToHashedValueFromPlainText(hashProviderFactory),
            };
        }
    }

    extension(BasicAuth basicAuth)
    {
        public BasicAuthEntity ToEntity()
        {
            return new BasicAuthEntity
            {
                Id = basicAuth.Id,
                UserId = basicAuth.UserId,
                Password = basicAuth.Password.ToHashString()!,
                ModifiedUtc = basicAuth.ModifiedUtc,
            };
        }
    }

    extension(BasicAuthEntity entity)
    {
        public BasicAuth ToModel(IHashProviderFactory hashProviderFactory)
        {
            return new BasicAuth
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Password = entity.Password.ToHashedValue(hashProviderFactory),
                ModifiedUtc = entity.ModifiedUtc,
            };
        }
    }
}
