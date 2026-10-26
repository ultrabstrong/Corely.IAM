using System.IdentityModel.Tokens.Jwt;

namespace Corely.IAM.Web.FunctionalTests.Infrastructure;

public static class TestJwt
{
    public static JwtSecurityToken Read(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    public static string GetJti(string token) => Read(token).Id;

    public static DateTime GetExpiresUtc(string token) => Read(token).ValidTo;
}
