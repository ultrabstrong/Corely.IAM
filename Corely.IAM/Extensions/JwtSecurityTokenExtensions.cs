using System.IdentityModel.Tokens.Jwt;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Extensions;

internal static class JwtSecurityTokenExtensions
{
    extension(JwtSecurityToken token)
    {
        public string? ClaimValue(string claimType) =>
            token.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;

        public Guid? SignedInAccountId() =>
            Guid.TryParse(
                token.ClaimValue(SessionTokenProvider.SIGNED_IN_ACCOUNT_ID_CLAIM),
                out var accountId
            )
                ? accountId
                : null;
    }
}
