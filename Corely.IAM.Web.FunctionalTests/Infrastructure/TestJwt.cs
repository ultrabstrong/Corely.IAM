using System.IdentityModel.Tokens.Jwt;

namespace Corely.IAM.Web.FunctionalTests.Infrastructure;

public static class TestJwt
{
    public const string SESSION_STARTED_AT = "session_started_at";

    public static JwtSecurityToken Read(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    public static string GetJti(string token) => Read(token).Id;

    public static DateTime GetExpiresUtc(string token) => Read(token).ValidTo;

    public static long? GetSessionStartedAt(string token)
    {
        var claim = Read(token).Claims.FirstOrDefault(c => c.Type == SESSION_STARTED_AT);
        return claim is null ? null : long.Parse(claim.Value);
    }
}
