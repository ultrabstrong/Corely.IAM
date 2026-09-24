using System.IdentityModel.Tokens.Jwt;
using Corely.IAM.Users.Constants;

namespace Corely.IAM.Extensions;

internal static class JwtSecurityTokenExtensions
{
    extension(JwtSecurityToken token)
    {
        public string? ClaimValue(string claimType) =>
            token.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;

        public DateTime? SessionStartedUtc()
        {
            var sessionStartedClaim = token.ClaimValue(UserConstants.SESSION_STARTED_AT_CLAIM);
            if (
                !string.IsNullOrWhiteSpace(sessionStartedClaim)
                && long.TryParse(sessionStartedClaim, out var sessionStartedUnix)
            )
            {
                return DateTimeOffset.FromUnixTimeSeconds(sessionStartedUnix).UtcDateTime;
            }

            var issuedAtClaim = token.ClaimValue(JwtRegisteredClaimNames.Iat);
            if (
                !string.IsNullOrWhiteSpace(issuedAtClaim)
                && long.TryParse(issuedAtClaim, out var issuedAt)
            )
                return DateTimeOffset.FromUnixTimeSeconds(issuedAt).UtcDateTime;

            return null;
        }

        public Guid? SignedInAccountId() =>
            Guid.TryParse(
                token.ClaimValue(UserConstants.SIGNED_IN_ACCOUNT_ID_CLAIM),
                out var accountId
            )
                ? accountId
                : null;
    }
}
