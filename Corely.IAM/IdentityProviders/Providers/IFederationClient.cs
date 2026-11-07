using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.IdentityProviders.Types;

namespace Corely.IAM.IdentityProviders.Providers;

internal interface IFederationClient
{
    Task<FederationAuthorization?> AuthorizeAsync(
        IdentityProviderDefinition definition,
        ResolvedIdentityProvider provider,
        FederatedSignInPurpose purpose,
        string redirectUri,
        DateTime utcNow
    );

    Task<ExternalIdentity?> RedeemAsync(
        IdentityProviderDefinition definition,
        ResolvedIdentityProvider provider,
        FederatedSignInCorrelation correlation,
        string code,
        string redirectUri,
        DateTime utcNow
    );
}
