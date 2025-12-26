using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.Security.Mappers;
using Corely.Security.Hashing.Factories;

namespace Corely.IAM.BasicAuths.Mappers;

internal static class BasicAuthMapper
{
    public static BasicAuth ToBasicAuth(
        this CreateBasicAuthRequest request,
        IHashProviderFactory hashProviderFactory
    )
    {
        return new BasicAuth
        {
            UserId = request.UserId,
            Password = request.Password.ToHashedValueFromPlainText(hashProviderFactory),
        };
    }

    public static BasicAuth ToBasicAuth(
        this UpdateBasicAuthRequest request,
        IHashProviderFactory hashProviderFactory
    )
    {
        return new BasicAuth
        {
            UserId = request.UserId,
            Password = request.Password.ToHashedValueFromPlainText(hashProviderFactory),
        };
    }

    public static BasicAuthEntity ToEntity(
        this BasicAuth basicAuth,
        IHashProviderFactory hashProviderFactory
    )
    {
        return new BasicAuthEntity
        {
            Id = basicAuth.Id,
            UserId = basicAuth.UserId,
            Password = basicAuth.Password.ToHashString()!,
            ModifiedUtc = basicAuth.ModifiedUtc,
        };
    }

    public static BasicAuth ToModel(
        this BasicAuthEntity entity,
        IHashProviderFactory hashProviderFactory
    )
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
