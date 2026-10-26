using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.Security.Models;

internal sealed record TokenIssueContext(
    UserEntity UserEntity,
    UserAsymmetricKeyEntity SignatureKey,
    List<Account> Accounts,
    Account? SignedInAccount
)
{
    public const string JWT_AUDIENCE = "Corely.IAM";
    public const string ACCOUNT_ID_CLAIM = "account_id";
    public const string SIGNED_IN_ACCOUNT_ID_CLAIM = "signed_in_account_id";
    public const string DEVICE_ID_CLAIM = "device_id";

    public List<Claim> Claims(string jti, DateTime issuedAt, string deviceId)
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
            new(DEVICE_ID_CLAIM, deviceId),
        };

        foreach (var account in Accounts)
        {
            claims.Add(new Claim(ACCOUNT_ID_CLAIM, account.Id.ToString()));
        }

        if (SignedInAccount != null)
        {
            claims.Add(new Claim(SIGNED_IN_ACCOUNT_ID_CLAIM, SignedInAccount.Id.ToString()));
        }

        return claims;
    }
}
