using System.IdentityModel.Tokens.Jwt;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;

namespace Corely.IAM.IdentityProviders.Types;

internal sealed class GoogleProviderDefinition : OpenIdConnectProviderDefinition
{
    public const string DISCOVERY_URL =
        "https://accounts.google.com/.well-known/openid-configuration";

    private static readonly string[] _issuers =
    [
        "https://accounts.google.com",
        "accounts.google.com",
    ];

    public override IdentityProviderType Type => IdentityProviderType.Google;
    public override string DisplayName => "Google";
    public override bool OffersAccountPicker => true;

    public override string DiscoveryUrl(IdentityProviderSettings settings) => DISCOVERY_URL;

    public override bool IsValidIssuer(
        string issuer,
        JwtSecurityToken token,
        string metadataIssuer
    ) => _issuers.Contains(issuer);

    public override bool IsEmailVerified(JwtSecurityToken token) =>
        token.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value is "true" or "True";
}
