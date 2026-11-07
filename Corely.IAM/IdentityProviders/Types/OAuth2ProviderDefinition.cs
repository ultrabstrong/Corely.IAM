using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;

namespace Corely.IAM.IdentityProviders.Types;

internal abstract class OAuth2ProviderDefinition : IdentityProviderDefinition
{
    public override FederationProtocol Protocol => FederationProtocol.OAuth2;
    public override EmailVerificationEvidence EmailEvidence => EmailVerificationEvidence.Claim;

    public abstract string Issuer { get; }
    public abstract string AuthorizationEndpoint { get; }
    public abstract string TokenEndpoint { get; }

    public virtual HttpRequestMessage TokenRequest(
        ResolvedIdentityProvider provider,
        string code,
        string redirectUri,
        string? codeVerifier
    )
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = provider.Settings.ClientId,
            ["client_secret"] = provider.ClientSecret ?? string.Empty,
        };
        if (codeVerifier is not null)
            form["code_verifier"] = codeVerifier;

        var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(form),
        };
        request.Headers.Accept.ParseAdd("application/json");
        return request;
    }

    public abstract Task<ExternalIdentity?> ReadIdentityAsync(
        string accessToken,
        ResolvedIdentityProvider provider,
        HttpClient http,
        CancellationToken cancellationToken
    );
}
