using System.IdentityModel.Tokens.Jwt;
using Corely.Common.Extensions;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Security.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.GoogleAuths.Providers;

internal class GoogleIdTokenValidator(
    IOptions<SecurityOptions> securityOptions,
    ILogger<GoogleIdTokenValidator> logger
) : IGoogleIdTokenValidator
{
    private const string GOOGLE_DISCOVERY_URL =
        "https://accounts.google.com/.well-known/openid-configuration";

    private static readonly string[] _validIssuers =
    [
        "accounts.google.com",
        "https://accounts.google.com",
    ];

    private readonly SecurityOptions _securityOptions = securityOptions
        .ThrowIfNull(nameof(securityOptions))
        .Value;
    private readonly ILogger<GoogleIdTokenValidator> _logger = logger.ThrowIfNull(nameof(logger));

    private ConfigurationManager<OpenIdConnectConfiguration>? _configManager;

    public async Task<GoogleIdTokenPayload?> ValidateAsync(string idToken)
    {
        if (string.IsNullOrWhiteSpace(_securityOptions.GoogleClientId))
        {
            _logger.LogWarning("Google sign-in attempted but GoogleClientId is not configured");
            return null;
        }

        try
        {
            var configManager = GetConfigurationManager();
            var openIdConfig = await configManager.GetConfigurationAsync(CancellationToken.None);

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = _validIssuers,
                ValidateAudience = true,
                ValidAudience = _securityOptions.GoogleClientId,
                ValidateLifetime = true,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ValidateIssuerSigningKey = true,
            };

            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(idToken, validationParams, out var validatedToken);

            var jwt = (JwtSecurityToken)validatedToken;
            var subject = jwt.Subject;
            var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty;
            var emailVerified =
                jwt.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value == "true";

            if (string.IsNullOrWhiteSpace(subject))
            {
                _logger.LogWarning("Google ID token has no subject claim");
                return null;
            }

            return new GoogleIdTokenPayload(subject, email, emailVerified);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogInformation("Google ID token validation failed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error validating Google ID token");
            return null;
        }
    }

    private ConfigurationManager<OpenIdConnectConfiguration> GetConfigurationManager()
    {
        return _configManager ??= new ConfigurationManager<OpenIdConnectConfiguration>(
            GOOGLE_DISCOVERY_URL,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever()
        );
    }
}
