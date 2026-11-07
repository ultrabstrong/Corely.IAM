namespace Corely.IAM.IdentityProviders.Models;

internal sealed record FederationAuthorization(string Url, FederatedSignInCorrelation Correlation);
