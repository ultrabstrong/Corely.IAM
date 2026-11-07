using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.UnitTests.IdentityProviders;

internal sealed class StubIdentityProvider : HttpMessageHandler
{
    public const string KEY_ID = "test-key";

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes =
        new(StringComparer.OrdinalIgnoreCase);

    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string> RequestBodies { get; } = [];

    public RsaSecurityKey SigningKey => new(_rsa) { KeyId = KEY_ID };

    public void ServeDiscovery(string discoveryUrl, string issuer, string? keyIssuer = null)
    {
        var baseUrl = new Uri(discoveryUrl).GetLeftPart(UriPartial.Authority);
        var jwksUrl = $"{baseUrl}/test-jwks";
        Json(
            discoveryUrl,
            new
            {
                issuer,
                authorization_endpoint = $"{baseUrl}/authorize",
                token_endpoint = $"{baseUrl}/token",
                jwks_uri = jwksUrl,
            }
        );

        var parameters = _rsa.ExportParameters(false);
        var key = new Dictionary<string, string>
        {
            ["kty"] = "RSA",
            ["use"] = "sig",
            ["kid"] = KEY_ID,
            ["n"] = Base64UrlEncoder.Encode(parameters.Modulus),
            ["e"] = Base64UrlEncoder.Encode(parameters.Exponent),
        };
        if (keyIssuer is not null)
            key["issuer"] = keyIssuer;
        Json(jwksUrl, new { keys = new[] { key } });
    }

    public string IdToken(
        string issuer,
        string audience,
        string subject,
        string? nonce,
        DateTime utcNow,
        IDictionary<string, object>? claims = null,
        int lifetimeSeconds = 300
    )
    {
        var tokenClaims = new List<Claim> { new("sub", subject) };
        if (nonce is not null)
            tokenClaims.Add(new("nonce", nonce));
        var token = new JwtSecurityToken(
            issuer,
            audience,
            tokenClaims,
            utcNow.AddSeconds(Math.Min(-1, lifetimeSeconds - 600)),
            utcNow.AddSeconds(lifetimeSeconds),
            new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256)
        );
        foreach (var claim in claims ?? new Dictionary<string, object>())
            token.Payload[claim.Key] = claim.Value;
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Json(string url, object body, HttpStatusCode status = HttpStatusCode.OK) =>
        _routes[url] = _ => new HttpResponseMessage(status)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            ),
        };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        Requests.Add(request);
        RequestBodies.Add(
            request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken)
        );
        var url = request.RequestUri!.GetLeftPart(UriPartial.Path);
        return _routes.TryGetValue(url, out var route)
            ? route(request)
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _rsa.Dispose();
        base.Dispose(disposing);
    }
}
