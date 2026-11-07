using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.Web.FunctionalTests.Infrastructure;

public sealed class StubGoogle : HttpMessageHandler
{
    public const string ISSUER = "https://accounts.google.com";
    public const string CLIENT_ID = "functional-google-client";
    public const string DISCOVERY_URL =
        "https://accounts.google.com/.well-known/openid-configuration";
    public const string TOKEN_URL = "https://accounts.google.com/token";
    private const string JWKS_URL = "https://accounts.google.com/test-jwks";
    private const string KEY_ID = "functional-key";

    private readonly RSA _rsa = RSA.Create(2048);

    public string? NextIdToken { get; set; }
    public int TokenRequests { get; private set; }

    public DateTime UtcNow { get; set; } = DateTime.UtcNow;

    public string IdToken(string subject, string email, string? nonce, bool emailVerified = true)
    {
        var claims = new List<Claim> { new("sub", subject), new("email", email) };
        if (nonce is not null)
            claims.Add(new("nonce", nonce));
        var token = new JwtSecurityToken(
            ISSUER,
            CLIENT_ID,
            claims,
            UtcNow.AddSeconds(-1),
            UtcNow.AddMinutes(5),
            new SigningCredentials(
                new RsaSecurityKey(_rsa) { KeyId = KEY_ID },
                SecurityAlgorithms.RsaSha256
            )
        );
        token.Payload["email_verified"] = emailVerified;
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var url = request.RequestUri!.GetLeftPart(UriPartial.Path);
        if (url == DISCOVERY_URL)
            return Json(
                new
                {
                    issuer = ISSUER,
                    authorization_endpoint = "https://accounts.google.com/authorize",
                    token_endpoint = TOKEN_URL,
                    jwks_uri = JWKS_URL,
                }
            );

        if (url == JWKS_URL)
        {
            var parameters = _rsa.ExportParameters(false);
            return Json(
                new
                {
                    keys = new[]
                    {
                        new
                        {
                            kty = "RSA",
                            use = "sig",
                            kid = KEY_ID,
                            n = Base64UrlEncoder.Encode(parameters.Modulus),
                            e = Base64UrlEncoder.Encode(parameters.Exponent),
                        },
                    },
                }
            );
        }

        if (url == TOKEN_URL)
        {
            TokenRequests++;
            return NextIdToken is null
                ? Json(new { error = "invalid_grant" }, HttpStatusCode.BadRequest)
                : Json(new { id_token = NextIdToken });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private static Task<HttpResponseMessage> Json(
        object body,
        HttpStatusCode status = HttpStatusCode.OK
    ) =>
        Task.FromResult(
            new HttpResponseMessage(status)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                ),
            }
        );

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _rsa.Dispose();
        base.Dispose(disposing);
    }
}
