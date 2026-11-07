using System.IdentityModel.Tokens.Jwt;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;

namespace Corely.IAM.IdentityProviders.Types;

internal abstract class OpenIdConnectProviderDefinition : IdentityProviderDefinition
{
    public override FederationProtocol Protocol => FederationProtocol.OpenIdConnect;
    public override EmailVerificationEvidence EmailEvidence => EmailVerificationEvidence.Claim;
    public override IReadOnlyList<string> Scopes { get; } = ["openid", "email", "profile"];

    public virtual bool ForcesFreshSignInForReauthentication => false;

    public abstract string DiscoveryUrl(IdentityProviderSettings settings);

    public virtual string ExpectedIssuer(string metadataIssuer) => metadataIssuer;

    public virtual bool IsValidIssuer(
        string issuer,
        JwtSecurityToken token,
        string metadataIssuer
    ) => issuer == metadataIssuer;

    public virtual bool IsValidSigningKeyIssuer(string? keyIssuer, JwtSecurityToken token) => true;

    public abstract bool IsEmailVerified(JwtSecurityToken token);
}
