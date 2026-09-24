using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Users.Constants;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.Security.Models;

internal sealed record TokenIssueContext(
    UserEntity UserEntity,
    UserAsymmetricKeyEntity SignatureKey,
    List<Account> Accounts,
    Account? SignedInAccount
)
{
    public List<Claim> Claims(
        string jti,
        DateTime issuedAt,
        DateTime sessionStartedUtc,
        string deviceId
    )
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, UserEntity.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
            new(
                UserConstants.SESSION_STARTED_AT_CLAIM,
                new DateTimeOffset(sessionStartedUtc).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
            new(UserConstants.DEVICE_ID_CLAIM, deviceId),
        };

        foreach (var account in Accounts)
        {
            claims.Add(new Claim(UserConstants.ACCOUNT_ID_CLAIM, account.Id.ToString()));
        }

        if (SignedInAccount != null)
        {
            claims.Add(
                new Claim(UserConstants.SIGNED_IN_ACCOUNT_ID_CLAIM, SignedInAccount.Id.ToString())
            );
        }

        return claims;
    }
}
