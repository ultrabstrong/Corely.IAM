using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.IdentityProviders.Types;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.IdentityProviders.Providers;

internal sealed class FederationClient(FederationHttp http, ILogger<FederationClient> logger)
    : IFederationClient
{
    public const int CLOCK_SKEW_SECONDS = 60;
    public const int CLIENT_ASSERTION_LIFETIME_SECONDS = 300;
    private const int RANDOM_BYTES = 32;
    private const string CLIENT_ASSERTION_TYPE =
        "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

    private readonly FederationHttp _http = http.ThrowIfNull(nameof(http));
    private readonly ILogger<FederationClient> _logger = logger.ThrowIfNull(nameof(logger));
    private readonly ConcurrentDictionary<
        string,
        ConfigurationManager<OpenIdConnectConfiguration>
    > _configurations = new();

    public async Task<FederationAuthorization?> AuthorizeAsync(
        IdentityProviderDefinition definition,
        ResolvedIdentityProvider provider,
        FederatedSignInPurpose purpose,
        string redirectUri,
        DateTime utcNow
    )
    {
        var state = NewRandomValue();
        var codeVerifier = NewRandomValue();
        string endpoint;
        string issuer;
        string? nonce = null;

        if (definition is OpenIdConnectProviderDefinition oidc)
        {
            var configuration = await GetConfigurationAsync(oidc, provider);
            if (configuration is null)
                return null;
            endpoint = configuration.AuthorizationEndpoint;
            issuer = oidc.ExpectedIssuer(configuration.Issuer);
            nonce = NewRandomValue();
        }
        else
        {
            var oauth = (OAuth2ProviderDefinition)definition;
            endpoint = oauth.AuthorizationEndpoint;
            issuer = oauth.Issuer;
        }

        var parameters = new List<KeyValuePair<string, string>>
        {
            new("response_type", "code"),
            new("client_id", provider.Settings.ClientId),
            new("redirect_uri", redirectUri),
            new("state", state),
        };
        if (definition.Scopes.Count > 0)
            parameters.Add(new("scope", string.Join(' ', definition.Scopes)));
        if (nonce is not null)
            parameters.Add(new("nonce", nonce));
        if (definition.UsesPkce)
        {
            parameters.Add(new("code_challenge", CodeChallenge(codeVerifier)));
            parameters.Add(new("code_challenge_method", "S256"));
        }
        parameters.AddRange(definition.AuthorizationParameters(purpose));

        var correlation = new FederatedSignInCorrelation(
            definition.Type,
            purpose,
            state,
            codeVerifier,
            nonce,
            issuer,
            utcNow
        );
        return new FederationAuthorization(AppendQuery(endpoint, parameters), correlation);
    }

    public async Task<ExternalIdentity?> RedeemAsync(
        IdentityProviderDefinition definition,
        ResolvedIdentityProvider provider,
        FederatedSignInCorrelation correlation,
        string code,
        string redirectUri,
        DateTime utcNow
    )
    {
        try
        {
            return definition switch
            {
                OpenIdConnectProviderDefinition oidc => await RedeemOpenIdConnectAsync(
                    oidc,
                    provider,
                    correlation,
                    code,
                    redirectUri,
                    utcNow
                ),
                OAuth2ProviderDefinition oauth => await RedeemOAuth2Async(
                    oauth,
                    provider,
                    correlation,
                    code,
                    redirectUri
                ),
                _ => null,
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(
                "Could not reach {IdentityProvider} to complete a sign in: {Message}",
                definition.Type,
                ex.Message
            );
            return null;
        }
    }

    internal static string CodeChallenge(string codeVerifier) =>
        Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

    private async Task<ExternalIdentity?> RedeemOpenIdConnectAsync(
        OpenIdConnectProviderDefinition definition,
        ResolvedIdentityProvider provider,
        FederatedSignInCorrelation correlation,
        string code,
        string redirectUri,
        DateTime utcNow
    )
    {
        var manager = Manager(definition, provider);
        var configuration = await GetConfigurationAsync(definition, provider);
        if (configuration is null)
            return null;

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = provider.Settings.ClientId,
            ["code_verifier"] = correlation.CodeVerifier,
        };
        if (provider.Settings.CredentialKind == IdentityProviderCredentialKind.Certificate)
        {
            form["client_assertion_type"] = CLIENT_ASSERTION_TYPE;
            form["client_assertion"] = ClientAssertion(
                provider,
                configuration.TokenEndpoint,
                utcNow
            );
        }
        else
        {
            form["client_secret"] = provider.ClientSecret ?? string.Empty;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, configuration.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(form),
        };
        request.Headers.Accept.ParseAdd("application/json");
        var idToken = await ReadTokenAsync(definition, request, "id_token");
        if (idToken is null)
            return null;

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return ValidateIdToken(
                    definition,
                    provider,
                    configuration,
                    correlation,
                    idToken,
                    utcNow
                );
            }
            catch (SecurityTokenSignatureKeyNotFoundException) when (attempt == 0)
            {
                manager.RequestRefresh();
                configuration = await manager.GetConfigurationAsync(CancellationToken.None);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogInformation(
                    "{IdentityProvider} returned an ID token that failed validation: {Message}",
                    definition.Type,
                    ex.Message
                );
                return null;
            }
        }
    }

    private ExternalIdentity? ValidateIdToken(
        OpenIdConnectProviderDefinition definition,
        ResolvedIdentityProvider provider,
        OpenIdConnectConfiguration configuration,
        FederatedSignInCorrelation correlation,
        string idToken,
        DateTime utcNow
    )
    {
        var parameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = provider.Settings.ClientId,
            ValidateIssuer = true,
            IssuerValidator = (issuer, token, _) =>
                definition.IsValidIssuer(issuer, (JwtSecurityToken)token, configuration.Issuer)
                    ? issuer
                    : throw new SecurityTokenInvalidIssuerException(
                        $"Issuer {issuer} is not one {definition.Type} issues tokens under"
                    ),
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyValidator = (key, token, _) =>
                definition.IsValidSigningKeyIssuer(
                    KeyIssuer(configuration, key),
                    (JwtSecurityToken)token
                ),
            ValidateLifetime = true,
            LifetimeValidator = (notBefore, expires, _, _) =>
                expires is { } expiresUtc
                && expiresUtc > utcNow.AddSeconds(-CLOCK_SKEW_SECONDS)
                && (notBefore is null || notBefore <= utcNow.AddSeconds(CLOCK_SKEW_SECONDS)),
        };

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        handler.ValidateToken(idToken, parameters, out var validated);
        var jwt = (JwtSecurityToken)validated;

        var nonce = Claim(jwt, "nonce");
        if (
            correlation.Nonce is null
            || nonce is null
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(nonce),
                Encoding.UTF8.GetBytes(correlation.Nonce)
            )
        )
        {
            _logger.LogInformation(
                "{IdentityProvider} returned an ID token without this sign in's nonce",
                definition.Type
            );
            return null;
        }

        if (string.IsNullOrWhiteSpace(jwt.Subject))
            return null;

        var email = Claim(jwt, "email");
        return new ExternalIdentity(
            definition.Type,
            jwt.Issuer,
            jwt.Subject,
            email,
            !string.IsNullOrWhiteSpace(email) && definition.IsEmailVerified(jwt),
            long.TryParse(Claim(jwt, "auth_time"), out var authTime)
                ? DateTimeOffset.FromUnixTimeSeconds(authTime).UtcDateTime
                : null
        );
    }

    private async Task<ExternalIdentity?> RedeemOAuth2Async(
        OAuth2ProviderDefinition definition,
        ResolvedIdentityProvider provider,
        FederatedSignInCorrelation correlation,
        string code,
        string redirectUri
    )
    {
        using var request = definition.TokenRequest(
            provider,
            code,
            redirectUri,
            definition.UsesPkce ? correlation.CodeVerifier : null
        );
        var accessToken = await ReadTokenAsync(definition, request, "access_token");
        return accessToken is null
            ? null
            : await definition.ReadIdentityAsync(
                accessToken,
                provider,
                _http.Client,
                CancellationToken.None
            );
    }

    private async Task<string?> ReadTokenAsync(
        IdentityProviderDefinition definition,
        HttpRequestMessage request,
        string tokenName
    )
    {
        using var response = await _http.Client.SendAsync(request);
        using var document = await TryParseAsync(response);
        if (
            response.IsSuccessStatusCode
            && document?.RootElement.TryGetProperty(tokenName, out var token) == true
            && token.ValueKind == JsonValueKind.String
        )
        {
            return token.GetString();
        }

        _logger.LogInformation(
            "{IdentityProvider} refused the authorization code ({StatusCode}): {Error}",
            definition.Type,
            (int)response.StatusCode,
            document?.RootElement.TryGetProperty("error", out var error) == true
                ? error.ToString()
                : "no error given"
        );
        return null;
    }

    private static async Task<JsonDocument?> TryParseAsync(HttpResponseMessage response)
    {
        try
        {
            return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<OpenIdConnectConfiguration?> GetConfigurationAsync(
        OpenIdConnectProviderDefinition definition,
        ResolvedIdentityProvider provider
    )
    {
        try
        {
            return await Manager(definition, provider)
                .GetConfigurationAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Could not read {IdentityProvider}'s OpenID Connect discovery document: {Message}",
                definition.Type,
                ex.Message
            );
            return null;
        }
    }

    private ConfigurationManager<OpenIdConnectConfiguration> Manager(
        OpenIdConnectProviderDefinition definition,
        ResolvedIdentityProvider provider
    ) =>
        _configurations.GetOrAdd(
            definition.DiscoveryUrl(provider.Settings),
            url => new ConfigurationManager<OpenIdConnectConfiguration>(
                url,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(_http.Client)
            )
        );

    private static string ClientAssertion(
        ResolvedIdentityProvider provider,
        string tokenEndpoint,
        DateTime utcNow
    )
    {
        using var certificate = provider.LoadCertificate();
        var token = new JwtSecurityToken(
            issuer: provider.Settings.ClientId,
            audience: tokenEndpoint,
            claims:
            [
                new Claim("sub", provider.Settings.ClientId),
                new Claim("jti", Guid.NewGuid().ToString()),
            ],
            notBefore: utcNow,
            expires: utcNow.AddSeconds(CLIENT_ASSERTION_LIFETIME_SECONDS),
            signingCredentials: new X509SigningCredentials(
                certificate,
                SecurityAlgorithms.RsaSha256
            )
        );
        token.Header["x5t#S256"] = Base64UrlEncoder.Encode(SHA256.HashData(certificate.RawData));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string? KeyIssuer(OpenIdConnectConfiguration configuration, SecurityKey key) =>
        configuration
            .JsonWebKeySet?.Keys.FirstOrDefault(k => k.Kid == key.KeyId)
            ?.AdditionalData.TryGetValue("issuer", out var issuer) == true
            ? issuer as string
            : null;

    private static string? Claim(JwtSecurityToken token, string type) =>
        token.Claims.FirstOrDefault(c => c.Type == type)?.Value;

    private static string NewRandomValue() =>
        Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RANDOM_BYTES));

    private static string AppendQuery(
        string endpoint,
        IEnumerable<KeyValuePair<string, string>> parameters
    ) =>
        endpoint
        + (endpoint.Contains('?') ? "&" : "?")
        + string.Join(
            "&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}")
        );
}
