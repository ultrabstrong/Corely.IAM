using Corely.IAM.Entities;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.Security.Hashing.Factories;

namespace Corely.IAM.Security.Extensions;

internal static class AuthTokenEntityExtensions
{
    extension(AuthTokenEntity token)
    {
        public bool HasRefreshSecret(string secret, IHashProviderFactory hashProviderFactory) =>
            !string.IsNullOrWhiteSpace(token.RefreshSecretHash)
            && token.RefreshSecretHash.ToHashedValue(hashProviderFactory).Verify(secret);

        public AuthTokenRenewalState RenewalStateAt(DateTime utcNow, SecurityPolicy policy) =>
            token.ReplacedByTokenId.HasValue
                ? token.RevokedUtc?.AddSeconds(policy.RefreshReuseGraceSeconds) > utcNow
                    ? AuthTokenRenewalState.RecentlyReplaced
                    : AuthTokenRenewalState.Reused
                : token.RevokedUtc != null
                    ? AuthTokenRenewalState.Revoked
                    : token.RefreshExpiresUtc <= utcNow
                    || token.SessionStartedUtc.AddSeconds(policy.AuthSessionTtlSeconds) <= utcNow
                        ? AuthTokenRenewalState.Expired
                        : AuthTokenRenewalState.Renewable;
    }
}
